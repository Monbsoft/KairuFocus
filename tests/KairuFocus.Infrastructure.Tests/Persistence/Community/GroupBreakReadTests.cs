using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Internal;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// <see cref="IOpenSprintReadRepository.FindGroupBreakAsync"/> (UC-CO-10): whether the group of a finished
/// sprint takes a break together, and for how long. Read across users, so the SQL is checked too (ADR-026).
/// </summary>
public sealed class GroupBreakReadTests
{
    private static readonly DateTime SprintEnd = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SprintStart = SprintEnd.AddMinutes(-25);
    private static readonly DateTime JoinedAt = SprintEnd.AddMinutes(-20);

    private readonly InMemoryDatabase _database = new();
    private readonly UserId _host = UserId.New();
    private readonly UserId _first = UserId.New();
    private readonly UserId _second = UserId.New();

    // --- Who makes a group ----------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnNull_When_RootIsUnknown()
    {
        await _database.SeedAsync(Profile(_first, "tomtom"));

        var rules = await FindAsync(Guid.NewGuid());

        Assert.Null(rules);
    }

    [Fact]
    public async Task Should_ReturnNull_When_OnlyTheHostTookPart()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(Profile(_host, "lea_dev"), root);

        Assert.Null(await FindAsync(root));
    }

    [Fact]
    public async Task Should_ReturnRules_When_HostAndOneParticipantTookPart()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            root,
            CompletedJoined(_first, root));

        Assert.NotNull(await FindAsync(root));
    }

    [Fact]
    public async Task Should_ReturnNull_When_TheOnlyOtherParticipantInterrupted()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            root,
            InterruptedJoined(_first, root));

        Assert.Null(await FindAsync(root));
    }

    // Two sessions that were not interrupted are enough, whoever left: the host may have gone.
    [Fact]
    public async Task Should_ReturnRules_When_HostInterruptedAndTwoParticipantsCompleted()
    {
        var root = InterruptedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            Profile(_second, "noa_42"),
            root,
            CompletedJoined(_first, root),
            CompletedJoined(_second, root));

        Assert.NotNull(await FindAsync(root));
    }

    [Fact]
    public async Task Should_ReturnNull_When_HostInterruptedAndOneParticipantCompleted()
    {
        var root = InterruptedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            root,
            CompletedJoined(_first, root));

        Assert.Null(await FindAsync(root));
    }

    // A session that is still Active counts: the read does not look at deadlines.
    [Fact]
    public async Task Should_ReturnRules_When_ParticipantsAreStillActive()
    {
        var root = StartedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            root,
            StartedJoined(_first, root));

        Assert.NotNull(await FindAsync(root));
    }

    [Fact]
    public async Task Should_ReturnNull_When_OtherParticipantIsNotMember()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(Profile(_host, "lea_dev"), root, CompletedJoined(_first, root));

        Assert.Null(await FindAsync(root));
    }

    [Fact]
    public async Task Should_ReturnNull_When_HostIsNotMemberAndOnlyOneParticipantIs()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(Profile(_first, "tomtom"), root, CompletedJoined(_first, root));

        Assert.Null(await FindAsync(root));
    }

    // The same owner twice in the group (a joined session, then another one) is one participant.
    [Fact]
    public async Task Should_CountOwnerOnce_When_OwnerHasSeveralSessionsInTheGroup()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_first, "tomtom"),
            root,
            CompletedJoined(_first, root),
            CompletedJoined(_first, root));

        Assert.Null(await FindAsync(root));
    }

    [Fact]
    public async Task Should_CountOwnerOnce_When_HostAlsoJoinedOwnGroup()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            root,
            CompletedJoined(_host, root));

        Assert.Null(await FindAsync(root));
    }

    // A break of the group carries the same root as its sprints, but it is not a sprint.
    [Fact]
    public async Task Should_NotCountBreaks_When_ParticipantsOnlyTookTheBreak()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            root,
            ActiveGroupBreak(_first, root));

        Assert.Null(await FindAsync(root));
    }

    [Fact]
    public async Task Should_IgnoreOtherGroups_When_ReadingOne()
    {
        var root = CompletedRoot(_host);
        var otherRoot = CompletedRoot(_second);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            Profile(_second, "noa_42"),
            root,
            otherRoot,
            CompletedJoined(_first, otherRoot));

        Assert.Null(await FindAsync(root));
        Assert.NotNull(await FindAsync(otherRoot));
    }

    // --- Length of the break --------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnBreakLengthOfTheHost_When_ParticipantsSetAnother()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            Profile(_second, "noa_42"),
            Settings(_first, shortBreakMinutes: 3),
            Settings(_host, shortBreakMinutes: 10),
            Settings(_second, shortBreakMinutes: 7),
            root,
            CompletedJoined(_first, root),
            CompletedJoined(_second, root));

        var rules = await FindAsync(root);

        Assert.Equal(new GroupBreakRules(10), rules);
    }

    [Fact]
    public async Task Should_ReturnDefaultBreakLength_When_HostHasNoSettings()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_host, "lea_dev"),
            Profile(_first, "tomtom"),
            Settings(_first, shortBreakMinutes: 3),
            root,
            CompletedJoined(_first, root));

        var rules = await FindAsync(root);

        Assert.Equal(new GroupBreakRules(PomodoroSettings.Default.ShortBreakDurationMinutes), rules);
    }

    // The host left the community: their settings still decide, the rest of the group is unchanged.
    [Fact]
    public async Task Should_ReturnBreakLengthOfTheHost_When_HostIsNotMember()
    {
        var root = CompletedRoot(_host);
        await _database.SeedAsync(
            Profile(_first, "tomtom"),
            Profile(_second, "noa_42"),
            Settings(_host, shortBreakMinutes: 12),
            root,
            CompletedJoined(_first, root),
            CompletedJoined(_second, root));

        var rules = await FindAsync(root);

        Assert.Equal(new GroupBreakRules(12), rules);
    }

    // --- SQL (ADR-026) --------------------------------------------------------------------------

    // The first command is the count of participants: the one that reads across users.
    [Fact]
    public async Task Should_NotReferenceUsersEmailOrDisplayName_When_ReadingGroupBreak()
    {
        var command = await CaptureAsync();

        Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Email", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DisplayName", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_KeepToMembersAndSprintsNotInterrupted_When_ReadingGroupBreak()
    {
        var command = await CaptureAsync();

        Assert.Matches(
            @"INNER JOIN \[MemberProfiles\] AS \[\w+\] ON \[\w+\]\.\[OwnerId\] = \[\w+\]\.\[UserId\]",
            command.Text);
        Assert.Contains("[SharedSprintId]", command.Text, StringComparison.Ordinal);
        Assert.Contains("[SessionType]", command.Text, StringComparison.Ordinal);
        Assert.Contains("[Status]", command.Text, StringComparison.Ordinal);
        Assert.Contains("N'Sprint'", command.Text, StringComparison.Ordinal);
        Assert.Contains("N'Interrupted'", command.Text, StringComparison.Ordinal);
    }

    // Only the number of owners leaves the query: no identifier is projected.
    [Fact]
    public async Task Should_ProjectOnlyACount_When_ReadingGroupBreak()
    {
        var command = await CaptureAsync();

        Assert.Single(command.OuterProjection);
        Assert.StartsWith("COUNT(*)", command.OuterProjection[0], StringComparison.Ordinal);
        Assert.Single(Occurrences(command.Text, "OwnerId"));
    }

    [Fact]
    public async Task Should_KeepDeadlineRulesOutOfSql_When_ReadingGroupBreak()
    {
        var command = await CaptureAsync();

        Assert.DoesNotContain("DATEADD", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DATEDIFF", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    // --- Helpers --------------------------------------------------------------------------------

    private static IEnumerable<int> Occurrences(string text, string term)
    {
        for (var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
             index >= 0;
             index = text.IndexOf(term, index + term.Length, StringComparison.OrdinalIgnoreCase))
        {
            yield return index;
        }
    }

    private static Task<CapturedCommand> CaptureAsync() =>
        SqlCommandCapture.CaptureAsync(context =>
            new EfCoreOpenSprintReadRepository(context).FindGroupBreakAsync(Guid.NewGuid()));

    private Task<GroupBreakRules?> FindAsync(PomodoroSession root) => FindAsync(root.Id.Value);

    private async Task<GroupBreakRules?> FindAsync(Guid rootSessionId)
    {
        await using var context = _database.CreateContext();
        return await new EfCoreOpenSprintReadRepository(context).FindGroupBreakAsync(rootSessionId);
    }

    private static MemberProfile Profile(UserId userId, string name) =>
        MemberProfile.Join(
            userId,
            ProfileName.Create(name).Value,
            ProfileNameSource.Chosen,
            avatarUrl: null,
            new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));

    private static PomodoroSettingsRow Settings(UserId userId, int shortBreakMinutes) =>
        new()
        {
            UserId = userId.Value.ToString(),
            SprintDurationMinutes = 25,
            ShortBreakDurationMinutes = shortBreakMinutes,
            LongBreakDurationMinutes = 15,
            DailySprintGoal = 4,
        };

    private static PomodoroSession StartedRoot(UserId owner)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, owner);
        Assert.True(session.Start(SprintStart).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedRoot(UserId owner)
    {
        var session = StartedRoot(owner);
        Assert.True(session.Complete(SprintEnd).IsSuccess);
        return session;
    }

    private static PomodoroSession InterruptedRoot(UserId owner)
    {
        var session = StartedRoot(owner);
        Assert.True(session.Interrupt(SprintStart.AddMinutes(10)).IsSuccess);
        return session;
    }

    private static PomodoroSession StartedJoined(UserId owner, PomodoroSession root)
    {
        var session = PomodoroSession.CreateJoined(owner, root.Id, SprintEnd, JoinedAt).Value;
        Assert.True(session.Start(JoinedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedJoined(UserId owner, PomodoroSession root)
    {
        var session = StartedJoined(owner, root);
        Assert.True(session.Complete(SprintEnd).IsSuccess);
        return session;
    }

    private static PomodoroSession InterruptedJoined(UserId owner, PomodoroSession root)
    {
        var session = StartedJoined(owner, root);
        Assert.True(session.Interrupt(SprintStart.AddMinutes(10)).IsSuccess);
        return session;
    }

    private static PomodoroSession ActiveGroupBreak(UserId owner, PomodoroSession root)
    {
        var created = PomodoroSession.CreateJoinedBreak(owner, root.Id, SprintEnd.AddMinutes(5), SprintEnd);
        Assert.True(created.IsSuccess);
        Assert.True(created.Value.Start(SprintEnd).IsSuccess);
        return created.Value;
    }
}
