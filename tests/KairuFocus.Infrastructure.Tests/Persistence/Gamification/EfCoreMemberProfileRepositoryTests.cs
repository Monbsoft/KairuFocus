using KairuFocus.Domain.Community;
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
public sealed class EfCoreMemberProfileRepositoryTests
{
    private const string Avatar = "https://avatars.githubusercontent.com/u/1";

    private static readonly DateTime JoinedAtUtc = new(2026, 10, 23, 9, 30, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_ReportNameTaken_When_NameBelongsToAnotherProfile()
    {
        await _database.SeedAsync(Profile(UserId.New(), "LoutreTenace"));

        await using var context = _database.CreateContext();
        var repository = new EfCoreMemberProfileRepository(context);

        Assert.True(await repository.IsNameTakenAsync(NameOf("LoutreTenace")));
        Assert.False(await repository.IsNameTakenAsync(NameOf("HibouCalme")));
    }

    [Fact]
    public async Task Should_ReportNameTaken_When_CaseDiffers()
    {
        await _database.SeedAsync(Profile(UserId.New(), "LoutreTenace"));

        await using var context = _database.CreateContext();
        var repository = new EfCoreMemberProfileRepository(context);

        Assert.True(await repository.IsNameTakenAsync(NameOf("loutretenace")));
        Assert.True(await repository.IsNameTakenAsync(NameOf("LOUTRETENACE")));
    }

    [Fact]
    public async Task Should_PersistProfile_When_AddSucceeds()
    {
        var userId = UserId.New();

        await using (var context = _database.CreateContext())
        {
            var added = await new EfCoreMemberProfileRepository(context)
                .TryAddAsync(Profile(userId, "LoutreTenace", ProfileNameSource.GitHub, Avatar));

            Assert.True(added);
        }

        var stored = await GetAsync(userId);
        Assert.NotNull(stored);
        Assert.Equal(NameOf("LoutreTenace"), stored.Name);
        Assert.Equal("loutretenace", stored.NameKey);
        Assert.Equal(ProfileNameSource.GitHub, stored.NameSource);
        Assert.Equal(Avatar, stored.AvatarUrl);
        Assert.Equal(JoinedAtUtc, stored.JoinedAtUtc);
    }

    [Fact]
    public async Task Should_PersistNewNameAndDropAvatar_When_UpdateSucceeds()
    {
        var userId = UserId.New();
        await _database.SeedAsync(Profile(userId, "LoutreTenace", ProfileNameSource.GitHub, Avatar));

        await using (var context = _database.CreateContext())
        {
            var repository = new EfCoreMemberProfileRepository(context);
            var profile = (await repository.GetByUserIdAsync(userId))!;
            profile.ChangeName(NameOf("HibouCalme"), ProfileNameSource.Chosen, avatarUrl: null);

            Assert.True(await repository.TryUpdateAsync(profile));
        }

        var stored = await GetAsync(userId);
        Assert.Equal(NameOf("HibouCalme"), stored?.Name);
        Assert.Equal("hiboucalme", stored?.NameKey);
        Assert.Equal(ProfileNameSource.Chosen, stored?.NameSource);
        Assert.Null(stored?.AvatarUrl);
    }

    [Fact]
    public async Task Should_DeleteProfile_When_Removed()
    {
        var userId = UserId.New();
        await _database.SeedAsync(Profile(userId, "LoutreTenace"));

        await using (var context = _database.CreateContext())
        {
            var repository = new EfCoreMemberProfileRepository(context);
            var profile = (await repository.GetByUserIdAsync(userId))!;

            Assert.True(await repository.TryRemoveAsync(profile));
        }

        Assert.Null(await GetAsync(userId));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_ProfileWasRemovedConcurrently()
    {
        var userId = UserId.New();
        await _database.SeedAsync(Profile(userId, "LoutreTenace"));

        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var first = new EfCoreMemberProfileRepository(firstContext);
        var second = new EfCoreMemberProfileRepository(secondContext);

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
        await _database.SeedAsync(Profile(userId, "LoutreTenace"));

        await using var renameContext = _database.CreateContext();
        await using var leaveContext = _database.CreateContext();
        var rename = new EfCoreMemberProfileRepository(renameContext);
        var leave = new EfCoreMemberProfileRepository(leaveContext);

        // A rename reads the profile, then a leave from another tab deletes it before the update.
        var profile = (await rename.GetByUserIdAsync(userId))!;
        Assert.True(await leave.TryRemoveAsync((await leave.GetByUserIdAsync(userId))!));

        profile.ChangeName(NameOf("HibouCalme"), ProfileNameSource.Chosen, avatarUrl: null);

        Assert.False(await rename.TryUpdateAsync(profile));

        // The rejected entity is detached: the re-read of the handler hits the database.
        Assert.Null(await rename.GetByUserIdAsync(userId));
        Assert.Null(await GetAsync(userId));
    }

    private async Task<MemberProfile?> GetAsync(UserId userId)
    {
        await using var context = _database.CreateContext();
        return await new EfCoreMemberProfileRepository(context).GetByUserIdAsync(userId);
    }

    private static MemberProfile Profile(
        UserId userId, string name, ProfileNameSource source = ProfileNameSource.Chosen, string? avatarUrl = null) =>
        MemberProfile.Join(userId, NameOf(name), source, avatarUrl, JoinedAtUtc);

    private static ProfileName NameOf(string value) => ProfileName.Create(value).Value;
}
