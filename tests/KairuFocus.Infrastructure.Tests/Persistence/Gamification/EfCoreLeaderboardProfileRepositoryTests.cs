using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

/// <summary>
/// Nominal paths and concurrent deletions. InMemory enforces no unique index, so the TryAdd / TryUpdate
/// uniqueness conflicts are covered by the Application fakes; it does reject an UPDATE / DELETE of a missing
/// row with DbUpdateConcurrencyException, like SQL Server's "0 rows affected" check, so the concurrent
/// deletions are reproduced here with two contexts (two simultaneous requests).
/// </summary>
public sealed class EfCoreLeaderboardProfileRepositoryTests
{
    private static readonly DateTime JoinedAtUtc = new(2026, 10, 23, 9, 30, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_ReportTaken_When_PseudonymBelongsToAnotherProfile()
    {
        await _database.SeedAsync(LeaderboardProfile.Join(UserId.New(), PseudonymOf("LoutreTenace"), JoinedAtUtc));

        await using var context = _database.CreateContext();
        var repository = new EfCoreLeaderboardProfileRepository(context);

        Assert.True(await repository.IsPseudonymTakenAsync(PseudonymOf("LoutreTenace")));
        Assert.False(await repository.IsPseudonymTakenAsync(PseudonymOf("HibouCalme")));
    }

    [Fact]
    public async Task Should_PersistProfile_When_AddSucceeds()
    {
        var userId = UserId.New();

        await using (var context = _database.CreateContext())
        {
            var added = await new EfCoreLeaderboardProfileRepository(context)
                .TryAddAsync(LeaderboardProfile.Join(userId, PseudonymOf("LoutreTenace"), JoinedAtUtc));

            Assert.True(added);
        }

        var stored = await GetAsync(userId);
        Assert.NotNull(stored);
        Assert.Equal(PseudonymOf("LoutreTenace"), stored.Pseudonym);
        Assert.Equal(JoinedAtUtc, stored.JoinedAtUtc);
    }

    [Fact]
    public async Task Should_PersistNewPseudonym_When_UpdateSucceeds()
    {
        var userId = UserId.New();
        await _database.SeedAsync(LeaderboardProfile.Join(userId, PseudonymOf("LoutreTenace"), JoinedAtUtc));

        await using (var context = _database.CreateContext())
        {
            var repository = new EfCoreLeaderboardProfileRepository(context);
            var profile = (await repository.GetByUserIdAsync(userId))!;
            profile.ChangePseudonym(PseudonymOf("HibouCalme"));

            Assert.True(await repository.TryUpdateAsync(profile));
        }

        var stored = await GetAsync(userId);
        Assert.Equal(PseudonymOf("HibouCalme"), stored?.Pseudonym);
    }

    [Fact]
    public async Task Should_DeleteProfile_When_Removed()
    {
        var userId = UserId.New();
        await _database.SeedAsync(LeaderboardProfile.Join(userId, PseudonymOf("LoutreTenace"), JoinedAtUtc));

        await using (var context = _database.CreateContext())
        {
            var repository = new EfCoreLeaderboardProfileRepository(context);
            var profile = (await repository.GetByUserIdAsync(userId))!;

            Assert.True(await repository.TryRemoveAsync(profile));
        }

        Assert.Null(await GetAsync(userId));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_ProfileWasRemovedConcurrently()
    {
        var userId = UserId.New();
        await _database.SeedAsync(LeaderboardProfile.Join(userId, PseudonymOf("LoutreTenace"), JoinedAtUtc));

        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var first = new EfCoreLeaderboardProfileRepository(firstContext);
        var second = new EfCoreLeaderboardProfileRepository(secondContext);

        // Two simultaneous leaves: both read the profile before either deletes it.
        var firstProfile = (await first.GetByUserIdAsync(userId))!;
        var secondProfile = (await second.GetByUserIdAsync(userId))!;

        Assert.True(await first.TryRemoveAsync(firstProfile));
        Assert.False(await second.TryRemoveAsync(secondProfile));

        // The rejected entity is detached: the context stays usable and answers from the database.
        Assert.Null(await second.GetByUserIdAsync(userId));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_UpdatedProfileWasRemovedConcurrently()
    {
        var userId = UserId.New();
        await _database.SeedAsync(LeaderboardProfile.Join(userId, PseudonymOf("LoutreTenace"), JoinedAtUtc));

        await using var rerollContext = _database.CreateContext();
        await using var leaveContext = _database.CreateContext();
        var reroll = new EfCoreLeaderboardProfileRepository(rerollContext);
        var leave = new EfCoreLeaderboardProfileRepository(leaveContext);

        // A reroll reads the profile, then a leave from another tab deletes it before the update.
        var profile = (await reroll.GetByUserIdAsync(userId))!;
        Assert.True(await leave.TryRemoveAsync((await leave.GetByUserIdAsync(userId))!));

        profile.ChangePseudonym(PseudonymOf("HibouCalme"));

        Assert.False(await reroll.TryUpdateAsync(profile));

        // The rejected entity is detached: the re-read of the handler hits the database.
        Assert.Null(await reroll.GetByUserIdAsync(userId));
        Assert.Null(await GetAsync(userId));
    }

    private async Task<LeaderboardProfile?> GetAsync(UserId userId)
    {
        await using var context = _database.CreateContext();
        return await new EfCoreLeaderboardProfileRepository(context).GetByUserIdAsync(userId);
    }

    private static Pseudonym PseudonymOf(string value) => Pseudonym.Create(value).Value;
}
