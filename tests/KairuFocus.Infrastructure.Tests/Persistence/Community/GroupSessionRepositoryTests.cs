using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// Round trip, lookups and conflicts of <see cref="EfCoreGroupSessionRepository"/>. InMemory enforces no
/// foreign key and raises no SQL Server error number: the SQL shape is covered by <see cref="GroupSessionSqlTests"/>.
/// The duplicate-key path of TryAddAsync is not testable under InMemory (a duplicate id surfaces as a raw
/// ArgumentException) and is checked at acceptance.
/// </summary>
public sealed class GroupSessionRepositoryTests
{
    private static readonly DateTime StartedAtUtc = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NowUtc = StartedAtUtc.AddMinutes(5);

    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_RoundTripGroup_When_Added()
    {
        var host = UserId.New();
        var member = UserId.New();
        var group = NewGroup(host, member);
        await using (var context = _database.CreateContext())
        {
            Assert.True(await new EfCoreGroupSessionRepository(context).TryAddAsync(group));
        }

        await using var readContext = _database.CreateContext();
        var loaded = await new EfCoreGroupSessionRepository(readContext).GetByIdAsync(group.Id);

        Assert.NotNull(loaded);
        Assert.Equal(group.Id, loaded.Id);
        Assert.Equal(1, loaded.CurrentStep.Number);
        Assert.Equal(GroupStepType.Sprint, loaded.CurrentStep.Type);
        Assert.Equal(StartedAtUtc, loaded.CurrentStep.StartedAtUtc);
        Assert.Equal(StartedAtUtc.AddMinutes(25), loaded.CurrentStep.EndsAtUtc);
        Assert.Equal(1, loaded.SprintCount);
        Assert.Equal(0, loaded.Version);
        Assert.False(loaded.IsClosedToJoin);
        Assert.Null(loaded.EndedAtUtc);
        Assert.Null(loaded.EndReason);
        Assert.Equal(1, loaded.ActiveMemberCount);
        Assert.True(loaded.IsHostedBy(host));
        Assert.False(loaded.IsHostedBy(member));
        Assert.True(loaded.IsActiveMember(member));
    }

    [Fact]
    public async Task Should_RoundTripEndedGroup_When_Updated()
    {
        var group = NewGroup(UserId.New(), UserId.New());
        await _database.SeedAsync(group);

        await using (var context = _database.CreateContext())
        {
            var repository = new EfCoreGroupSessionRepository(context);
            var tracked = await repository.GetByIdAsync(group.Id);
            Assert.True(tracked!.Stop(GroupSessionEndReason.Stopped, NowUtc).IsSuccess);
            Assert.True(await repository.TryUpdateAsync(tracked));
        }

        await using var readContext = _database.CreateContext();
        var loaded = await new EfCoreGroupSessionRepository(readContext).GetByIdAsync(group.Id);

        Assert.Equal(NowUtc, loaded!.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.Stopped, loaded.EndReason);
        Assert.Equal(1, loaded.Version);
    }

    [Fact]
    public async Task Should_ReturnNull_When_GroupDoesNotExist()
    {
        await using var context = _database.CreateContext();

        Assert.Null(await new EfCoreGroupSessionRepository(context).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Should_FindLatestGroup_When_UserIsHost()
    {
        var host = UserId.New();
        var group = NewGroup(host, UserId.New());
        await _database.SeedAsync(group, NewGroup(UserId.New(), UserId.New()));

        await using var context = _database.CreateContext();
        var found = await new EfCoreGroupSessionRepository(context).FindLatestForUserAsync(host);

        Assert.NotNull(found);
        Assert.Equal(group.Id, found.Id);
    }

    [Fact]
    public async Task Should_FindLatestGroup_When_UserIsActiveMember()
    {
        var member = UserId.New();
        var group = NewGroup(UserId.New(), member);
        await _database.SeedAsync(group, NewGroup(UserId.New(), UserId.New()));

        await using var context = _database.CreateContext();
        var found = await new EfCoreGroupSessionRepository(context).FindLatestForUserAsync(member);

        Assert.NotNull(found);
        Assert.Equal(group.Id, found.Id);
        Assert.True(found.IsActiveMember(member));
    }

    [Fact]
    public async Task Should_ReturnNull_When_MemberLeft()
    {
        var member = UserId.New();
        var group = NewGroup(UserId.New(), member);
        Assert.True(group.Leave(member, NowUtc.AddMinutes(1)).IsSuccess);
        await _database.SeedAsync(group);

        await using var context = _database.CreateContext();

        Assert.Null(await new EfCoreGroupSessionRepository(context).FindLatestForUserAsync(member));
    }

    [Fact]
    public async Task Should_ReturnNull_When_UserIsUnknown()
    {
        await _database.SeedAsync(NewGroup(UserId.New(), UserId.New()));

        await using var context = _database.CreateContext();

        Assert.Null(await new EfCoreGroupSessionRepository(context).FindLatestForUserAsync(UserId.New()));
    }

    [Fact]
    public async Task Should_ReturnMostRecent_When_UserHasTwoGroups()
    {
        var host = UserId.New();
        var older = NewGroup(host, UserId.New(), StartedAtUtc.AddHours(-3));
        var newer = NewGroup(host, UserId.New(), StartedAtUtc);
        await _database.SeedAsync(older, newer);

        await using var context = _database.CreateContext();
        var found = await new EfCoreGroupSessionRepository(context).FindLatestForUserAsync(host);

        Assert.NotNull(found);
        Assert.Equal(newer.Id, found.Id);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_VersionIsStale()
    {
        var group = NewGroup(UserId.New(), UserId.New());
        await _database.SeedAsync(group);

        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();
        var firstRepository = new EfCoreGroupSessionRepository(first);
        var secondRepository = new EfCoreGroupSessionRepository(second);
        var seenByFirst = await firstRepository.GetByIdAsync(group.Id);
        var seenBySecond = await secondRepository.GetByIdAsync(group.Id);

        Assert.True(seenByFirst!.Stop(GroupSessionEndReason.Stopped, NowUtc).IsSuccess);
        Assert.True(await firstRepository.TryUpdateAsync(seenByFirst));

        Assert.True(seenBySecond!.Close().IsSuccess);
        Assert.False(await secondRepository.TryUpdateAsync(seenBySecond));
    }

    private static GroupSession NewGroup(UserId host, UserId member, DateTime? startedAtUtc = null)
    {
        var start = startedAtUtc ?? StartedAtUtc;
        var result = GroupSession.Start(Guid.NewGuid(), host, start, start.AddMinutes(25), member, start.AddMinutes(5));
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
