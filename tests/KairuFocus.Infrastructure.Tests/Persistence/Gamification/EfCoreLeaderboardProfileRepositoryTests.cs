using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

/// <summary>
/// Nominal paths only: InMemory enforces no unique index, so the TryAdd / TryUpdate conflicts are covered
/// by the Application fakes.
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

            await repository.RemoveAsync(profile);
        }

        Assert.Null(await GetAsync(userId));
    }

    private async Task<LeaderboardProfile?> GetAsync(UserId userId)
    {
        await using var context = _database.CreateContext();
        return await new EfCoreLeaderboardProfileRepository(context).GetByUserIdAsync(userId);
    }

    private static Pseudonym PseudonymOf(string value) => Pseudonym.Create(value).Value;
}
