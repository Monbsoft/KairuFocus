using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// The open sprints nobody joined yet (no <c>GroupSessions</c> row). Groups are covered by
/// <see cref="OpenSprintGroupReadTests"/>.
/// </summary>
public sealed class EfCoreOpenSprintReadRepositoryTests
{
    private const int MaxGroups = 10;

    private static readonly DateTime Now = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();

    // --- Open sprints without group -------------------------------------------------------------

    [Fact]
    public async Task Should_Exclude_When_SoloSprintIsClosed()
    {
        var host = UserId.New();
        var root = Sprint(host, Now.AddMinutes(-5), plannedMinutes: 25);
        Assert.True(root.CloseToJoin().IsSuccess);
        await _database.SeedAsync(Profile(host, "lea_dev"), root);

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Empty(groups);
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Name("lea_dev"), Now)));
    }

    [Fact]
    public async Task Should_Exclude_When_SprintIsFreeOrBreak()
    {
        var (free, shortBreak, longBreak) = (UserId.New(), UserId.New(), UserId.New());
        await _database.SeedAsync(
            Profile(free, "lea_dev"),
            Profile(shortBreak, "tomtom"),
            Profile(longBreak, "noa_42"),
            Sprint(free, Now.AddMinutes(-5), plannedMinutes: 0),
            Started(shortBreak, PomodoroSessionType.ShortBreak, 5, Now.AddMinutes(-1)),
            Started(longBreak, PomodoroSessionType.LongBreak, 15, Now.AddMinutes(-1)));

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Empty(groups);
    }

    [Fact]
    public async Task Should_Exclude_When_SessionIsNotActive()
    {
        var (completed, interrupted, planned) = (UserId.New(), UserId.New(), UserId.New());
        var done = Sprint(completed, Now.AddMinutes(-5), plannedMinutes: 25);
        Assert.True(done.Complete(Now.AddMinutes(-1)).IsSuccess);
        var stopped = Sprint(interrupted, Now.AddMinutes(-5), plannedMinutes: 25);
        Assert.True(stopped.Interrupt(Now.AddMinutes(-1)).IsSuccess);
        await _database.SeedAsync(
            Profile(completed, "lea_dev"),
            Profile(interrupted, "tomtom"),
            Profile(planned, "noa_42"),
            done,
            stopped,
            PomodoroSession.Create(PomodoroSessionType.Sprint, 25, planned));

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Empty(groups);
    }

    // Nothing closes an expired session on the server: it stays Active until the client observes it.
    [Fact]
    public async Task Should_Exclude_When_DeadlineIsPastButStillActive()
    {
        var (expired, exactly, participant) = (UserId.New(), UserId.New(), UserId.New());
        await _database.SeedAsync(
            Profile(expired, "lea_dev"),
            Profile(exactly, "tomtom"),
            Profile(participant, "noa_42"),
            Sprint(expired, Now.AddMinutes(-30), plannedMinutes: 25),
            Sprint(exactly, Now.AddMinutes(-25), plannedMinutes: 25),
            Joined(participant, PomodoroSessionId.New(), Now.AddSeconds(-1), Now.AddMinutes(-5)));

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Empty(groups);
    }

    [Fact]
    public async Task Should_Exclude_When_OwnerIsNotMember()
    {
        await _database.SeedAsync(Sprint(UserId.New(), Now.AddMinutes(-5), plannedMinutes: 25));

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Empty(groups);
    }

    [Fact]
    public async Task Should_KeepTenWithMostTimeLeft_When_MoreGroupsExist()
    {
        var seed = new List<object>();
        for (var minutesLeft = 1; minutesLeft <= 12; minutesLeft++)
        {
            var host = UserId.New();
            seed.Add(Profile(host, $"member_{minutesLeft:00}"));
            seed.Add(Sprint(host, Now.AddMinutes(minutesLeft - 25), plannedMinutes: 25));
        }

        await _database.SeedAsync([.. seed]);

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Equal(
            Enumerable.Range(3, 10).Reverse().Select(minutesLeft => $"member_{minutesLeft:00}"),
            groups.Select(g => g.Lead.Name.Value));
    }

    [Fact]
    public async Task Should_OrderByOrdinalName_When_GroupsEndTogether()
    {
        var (lower, upper) = (UserId.New(), UserId.New());
        await _database.SeedAsync(
            Profile(lower, "alice"),
            Profile(upper, "Bob"),
            Sprint(lower, Now.AddMinutes(-5), plannedMinutes: 25),
            Sprint(upper, Now.AddMinutes(-5), plannedMinutes: 25));

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Equal(["Bob", "alice"], groups.Select(g => g.Lead.Name.Value));
    }

    // --- Joinable sprint of a name --------------------------------------------------------------

    [Fact]
    public async Task Should_FindOwnSprint_When_TargetIsHost()
    {
        var host = UserId.New();
        var root = Sprint(host, Now.AddMinutes(-5), plannedMinutes: 25);
        await _database.SeedAsync(Profile(host, "lea_dev"), root);

        var sprint = await QueryAsync(r => r.FindJoinableAsync(Name("lea_dev"), Now));

        Assert.Equal(new JoinableSprint(root.Id.Value, Now.AddMinutes(20), GroupStepType.Sprint, GroupPhase.Running), sprint);
    }

    [Fact]
    public async Task Should_FindRegardlessOfCase_When_NameDiffersByCase()
    {
        var host = UserId.New();
        await _database.SeedAsync(Profile(host, "lea_dev"), Sprint(host, Now.AddMinutes(-5), plannedMinutes: 25));

        var sprint = await QueryAsync(r => r.FindJoinableAsync(Name("LEA_DEV"), Now));

        Assert.NotNull(sprint);
    }

    [Fact]
    public async Task Should_NotFind_When_NameIsHeldByNoMember()
    {
        await _database.SeedAsync(Sprint(UserId.New(), Now.AddMinutes(-5), plannedMinutes: 25));

        var sprint = await QueryAsync(r => r.FindJoinableAsync(Name("unknown_name"), Now));

        Assert.Null(sprint);
    }

    [Fact]
    public async Task Should_NotFind_When_TargetSprintIsExpiredFreeOrBreak()
    {
        var (expired, free, onBreak) = (UserId.New(), UserId.New(), UserId.New());
        await _database.SeedAsync(
            Profile(expired, "lea_dev"),
            Profile(free, "tomtom"),
            Profile(onBreak, "noa_42"),
            Sprint(expired, Now.AddMinutes(-30), plannedMinutes: 25),
            Sprint(free, Now.AddMinutes(-5), plannedMinutes: 0),
            Started(onBreak, PomodoroSessionType.ShortBreak, 5, Now.AddMinutes(-1)));

        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Name("lea_dev"), Now)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Name("tomtom"), Now)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Name("noa_42"), Now)));
    }

    // --- Helpers --------------------------------------------------------------------------------

    private static ProfileName Name(string value) => ProfileName.Create(value).Value;

    private static MemberProfile Profile(UserId userId, string name, string? avatarUrl = null) =>
        MemberProfile.Join(
            userId,
            Name(name),
            avatarUrl is null ? ProfileNameSource.Chosen : ProfileNameSource.GitHub,
            avatarUrl,
            new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));

    private static PomodoroSession Sprint(UserId owner, DateTime startedAt, int plannedMinutes) =>
        Started(owner, PomodoroSessionType.Sprint, plannedMinutes, startedAt);

    private static PomodoroSession Started(UserId owner, PomodoroSessionType type, int plannedMinutes, DateTime startedAt)
    {
        var session = PomodoroSession.Create(type, plannedMinutes, owner);
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession Joined(UserId owner, PomodoroSessionId rootId, DateTime sharedEndAt, DateTime startedAt)
    {
        var joined = PomodoroSession.CreateJoinedStep(PomodoroSessionType.Sprint, owner, rootId, sharedEndAt, startedAt);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(startedAt).IsSuccess);
        return joined.Value;
    }

    private async Task<T> QueryAsync<T>(Func<EfCoreOpenSprintReadRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreOpenSprintReadRepository(context));
    }
}
