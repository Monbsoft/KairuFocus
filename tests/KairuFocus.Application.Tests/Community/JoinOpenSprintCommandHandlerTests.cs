using KairuFocus.Application.Community.Commands.JoinOpenSprint;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class JoinOpenSprintCommandHandlerTests
{
    private const string TargetName = "lea_dev";
    private const string VisitorName = "tomtom";

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly DateTime EndsAtUtc = NowUtc.AddMinutes(18).AddSeconds(24);
    private static readonly UserId Visitor = FakeCurrentUserService.TestUserId;
    private static readonly UserId Host = UserId.From(new Guid("20000000-0000-0000-0000-000000000002"));
    private static readonly UserId OtherMember = UserId.From(new Guid("30000000-0000-0000-0000-000000000003"));

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly FakeJournalEntryRepository _journal = new();
    private readonly FakeMediator _mediator;

    public JoinOpenSprintCommandHandlerTests()
    {
        _mediator = new FakeMediator(_journal);
    }

    /// <summary>Id of the host's first sprint, which becomes the id of the group. Replaced by the id of the
    /// seeded root session when there is one.</summary>
    private Guid RootId { get; set; } = new("10000000-0000-0000-0000-000000000001");

    /// <summary>The sessions of the visitor: the host's root session lives in the same repository.</summary>
    private List<PomodoroSession> VisitorSessions => _sessions.Sessions.Where(s => s.OwnerId == Visitor).ToList();

    private JoinOpenSprintCommandHandler CreateSut(TimeProvider? timeProvider = null) =>
        new(
            _profiles,
            _openSprints,
            _sessions,
            _groups,
            _mediator,
            new FakeCurrentUserService(),
            timeProvider ?? new FixedTimeProvider(Now),
            NullLogger<JoinOpenSprintCommandHandler>.Instance);

    private void GivenVisitorIsMember() => _profiles.Seed(Visitor, VisitorName);

    /// <summary>The host runs a 25-minute sprint ending at <paramref name="endsAtUtc"/>, nobody joined it yet.</summary>
    private PomodoroSession GivenTargetIsJoinable(DateTime? endsAtUtc = null)
    {
        var end = endsAtUtc ?? EndsAtUtc;
        var root = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Host);
        Assert.True(root.Start(end.AddMinutes(-25)).IsSuccess);
        _sessions.Sessions.Add(root);
        RootId = root.Id.Value;
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(RootId, end, GroupStepType.Sprint, GroupPhase.Running));
        return root;
    }

    /// <summary>The group of the target, already joined by <see cref="OtherMember"/>, in its first sprint.</summary>
    private GroupSession GivenGroupRunning()
    {
        var root = GivenTargetIsJoinable();
        var group = GroupSession.Start(RootId, Host, root.StartedAt!.Value, EndsAtUtc, OtherMember, NowUtc.AddMinutes(-3));
        Assert.True(group.IsSuccess);
        _groups.Seed(group.Value);
        return group.Value;
    }

    private static PomodoroSession JoinedSession(Guid rootId, DateTime startedAt, DateTime? sharedEndAt = null)
    {
        var joined = PomodoroSession.CreateJoinedStep(PomodoroSessionType.Sprint, Visitor,
            PomodoroSessionId.From(rootId),
            sharedEndAt ?? EndsAtUtc,
            startedAt);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(startedAt).IsSuccess);
        return joined.Value;
    }

    private static PomodoroSession OwnSprint(DateTime startedAt, int plannedMinutes = 25)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedMinutes, Visitor);
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    // --- Nominal -------------------------------------------------------------------------------

    [Fact]
    public async Task Should_CreateStartedJoinedSession_When_TargetIsJoinable()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);

        var session = Assert.Single(VisitorSessions);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
        Assert.Equal(PomodoroSessionType.Sprint, session.SessionType);
        Assert.Equal(PomodoroSessionId.From(RootId), session.SharedSprintId);
        Assert.Equal(EndsAtUtc, session.SharedEndAt);
        Assert.Equal(19, session.PlannedDurationMinutes); // 18:24 rounded up.
        Assert.Equal(NowUtc, session.StartedAt);

        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintStarted, entry.EventType);
        Assert.Equal(session.Id.Value, entry.ResourceId);
        Assert.Equal(NowUtc, entry.OccurredAt);
        Assert.Equal(Visitor, entry.OwnerId);
    }

    // The XP rule (at least 5 effective minutes) decides later: joining stays allowed.
    [Fact]
    public async Task Should_Allow_When_LessThanFiveMinutesRemain()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable(NowUtc.AddMinutes(4));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, Assert.Single(VisitorSessions).PlannedDurationMinutes);
    }

    // ADR-023: the session is persisted, a failing journal never fails the join.
    [Fact]
    public async Task Should_Succeed_When_JournalFails()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _mediator.ThrowOnCreateEntry = true;

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Single(VisitorSessions);
        Assert.Empty(_journal.Entries);
    }

    // V5: joining during a break enters the break, with the same end as the group.
    [Theory]
    [InlineData(GroupStepType.ShortBreak, PomodoroSessionType.ShortBreak)]
    [InlineData(GroupStepType.LongBreak, PomodoroSessionType.LongBreak)]
    public async Task Should_JoinBreak_When_GroupInBreak(GroupStepType stepType, PomodoroSessionType expectedType)
    {
        GivenVisitorIsMember();
        // First sprint 13:25-13:50, then the host launched the break at 13:59.
        var group = GroupSession.Start(RootId, Host, NowUtc.AddMinutes(-35), NowUtc.AddMinutes(-10), OtherMember, NowUtc.AddMinutes(-30)).Value;
        Assert.True(group.LaunchStep(stepType, NowUtc.AddMinutes(-1), EndsAtUtc, NowUtc.AddMinutes(-1)).IsSuccess);
        _groups.Seed(group);
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(RootId, EndsAtUtc, stepType, GroupPhase.Running));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);
        Assert.True(group.IsActiveMember(Visitor));
        var session = Assert.Single(VisitorSessions);
        Assert.Equal(expectedType, session.SessionType);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
        Assert.Equal(PomodoroSessionId.From(RootId), session.SharedSprintId);
        Assert.Equal(EndsAtUtc, session.SharedEndAt);
        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.BreakStarted, entry.EventType);
        Assert.Equal(session.Id.Value, entry.ResourceId);
    }

    // --- Refusals ------------------------------------------------------------------------------

    [Fact]
    public async Task Should_RefuseJoin_When_GroupWaitsForHost()
    {
        GivenVisitorIsMember();
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(RootId, NowUtc.AddMinutes(-2), GroupStepType.Sprint, GroupPhase.WaitingForHost));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupWaitingForHost, result.Error);
        Assert.Null(result.SharedEndAt);
        Assert.Empty(VisitorSessions);
        Assert.Empty(_groups.Groups);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_VisitorHasNoProfile()
    {
        GivenTargetIsJoinable();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Null(result.SharedEndAt);
        Assert.Empty(VisitorSessions);
        Assert.Empty(_groups.Groups);
    }

    // Unknown, not a member, closed or ended: the read returns nothing, and all of them look the same.
    [Theory]
    [InlineData("nobody")]        // Unknown or closed: the read finds nothing.
    [InlineData("ab")]            // Malformed: below the minimum length.
    [InlineData("not a name")]    // Malformed: forbidden characters.
    [InlineData("")]              // Malformed: empty.
    [InlineData("admin")]         // Reserved: nobody can hold it.
    public async Task Should_ReturnNotJoinable_When_TargetIsUnknownClosedOrMalformed(string target)
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(target));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.Empty(VisitorSessions);
        Assert.Empty(_groups.Groups);
    }

    [Theory]
    [InlineData(VisitorName)]
    [InlineData("TomTom")]
    public async Task Should_ReturnOwnSprint_When_TargetIsVisitor(string target)
    {
        GivenVisitorIsMember();
        _openSprints.SeedJoinable(VisitorName, new JoinableSprint(RootId, EndsAtUtc, GroupStepType.Sprint, GroupPhase.Running));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(target));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.CannotJoinOwnSprint, result.Error);
        Assert.Empty(VisitorSessions);
        Assert.Empty(_groups.Groups);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorHasActiveSession()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(OwnSprint(NowUtc.AddMinutes(-10)));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.Single(VisitorSessions);
        Assert.Empty(_journal.Entries);
    }

    // A timed session never observed past its end stays Active: the unique index still sees it.
    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorSessionIsExpiredButActive()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(OwnSprint(NowUtc.AddHours(-2), plannedMinutes: 25));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // --- Idempotence ---------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnExistingSession_When_SameRootWithinFiveSeconds()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var existing = JoinedSession(RootId, NowUtc.AddSeconds(-2));
        _sessions.Sessions.Add(existing);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);
        Assert.Same(existing, Assert.Single(VisitorSessions));
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_SameRootAfterFiveSeconds()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(JoinedSession(RootId, NowUtc.AddSeconds(-6)));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_OtherRootWithinFiveSeconds()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(JoinedSession(Guid.NewGuid(), NowUtc.AddSeconds(-2)));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // The visitor hosts the group of the target: the target joined the visitor's own sprint.
    // Same root, but the visitor did not join it and has no shared end to return.
    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorHostsTheTargetGroupWithinFiveSeconds()
    {
        GivenVisitorIsMember();
        var hosted = OwnSprint(NowUtc.AddSeconds(-2));
        _sessions.Sessions.Add(hosted);
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(hosted.Id.Value, EndsAtUtc, GroupStepType.Sprint, GroupPhase.Running));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // The other request inserted its row while this one was reading: a timestamp sampled before the read
    // would give a negative age, and a double click would look like a conflict.
    [Fact]
    public async Task Should_ReturnExistingSession_When_SessionWasInsertedDuringTheRead()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(JoinedSession(RootId, NowUtc.AddMilliseconds(500)));
        var clock = new SteppingTimeProvider(Now, TimeSpan.FromSeconds(1));

        var result = await CreateSut(clock).Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Single(VisitorSessions);
    }

    [Fact]
    public async Task Should_ReturnExistingSession_When_InsertRejectedForSameRoot()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var winner = JoinedSession(RootId, NowUtc.AddSeconds(-1));
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);
        Assert.Same(winner, Assert.Single(VisitorSessions));
        Assert.Empty(_journal.Entries);
        // A resubmission: the membership recorded for the winning request stays.
        Assert.True(Assert.Single(_groups.Groups).IsActiveMember(Visitor));
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_InsertRejectedForOtherRoot()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var winner = JoinedSession(Guid.NewGuid(), NowUtc.AddSeconds(-1));
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    [Fact]
    public async Task Should_ReturnConcurrentStart_When_InsertRejectedAndNothingReadable()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var winner = OwnSprint(NowUtc);
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);
        _sessions.OnInsertRejected = () => Assert.True(winner.Interrupt(NowUtc).IsSuccess);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.ConcurrentSessionStart, result.Error);
        Assert.Empty(_journal.Entries);
        Assert.False(Assert.Single(_groups.Groups).IsActiveMember(Visitor));
    }

    // --- Group session (UC-CO-07) -----------------------------------------------------------------

    [Fact]
    public async Task Should_CreateGroup_When_FirstMemberJoins()
    {
        GivenVisitorIsMember();
        var root = GivenTargetIsJoinable();
        var groupStoredBeforeSession = false;
        _sessions.OnTryAdd = () => groupStoredBeforeSession = _groups.Groups.Any(g => g.IsActiveMember(Visitor));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        var group = Assert.Single(_groups.Groups);
        Assert.Equal(RootId, group.Id);
        Assert.True(group.IsHostedBy(Host));
        Assert.True(group.IsActiveMember(Visitor));
        Assert.Equal(1, group.ActiveMemberCount);
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Equal(GroupStepType.Sprint, group.CurrentStep.Type);
        Assert.Equal(root.StartedAt, group.CurrentStep.StartedAtUtc);
        Assert.Equal(root.ScheduledEndAt, group.CurrentStep.EndsAtUtc);
        Assert.Null(group.EndedAtUtc);
        // Decision 2: the group is persisted before the Pomodoro session.
        Assert.True(groupStoredBeforeSession);
        Assert.Equal(PomodoroSessionId.From(RootId), Assert.Single(VisitorSessions).SharedSprintId);
    }

    [Fact]
    public async Task Should_JoinExistingGroup_When_GroupRunning()
    {
        GivenVisitorIsMember();
        var group = GivenGroupRunning();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);
        Assert.Same(group, Assert.Single(_groups.Groups));
        Assert.Equal(2, group.ActiveMemberCount);
        Assert.True(group.IsActiveMember(Visitor));
        Assert.Equal(1, _groups.JoinCallCount);
        Assert.Equal(0, _groups.UpdateCallCount);
        var session = Assert.Single(VisitorSessions);
        Assert.Equal(PomodoroSessionId.From(RootId), session.SharedSprintId);
        Assert.Equal(EndsAtUtc, session.SharedEndAt);
    }

    // The visitor waits for the host of another group to launch its next step.
    [Fact]
    public async Task Should_RefuseJoin_When_AlreadyInAnotherLiveGroup()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var other = GroupSession.Start(Guid.NewGuid(), UserId.New(), NowUtc.AddMinutes(-30), NowUtc.AddMinutes(-5), Visitor, NowUtc.AddMinutes(-20)).Value;
        _groups.Seed(other);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.AlreadyInGroup, result.Error);
        Assert.Null(result.SharedEndAt);
        Assert.Same(other, Assert.Single(_groups.Groups));
        Assert.True(other.IsActiveMember(Visitor));
        Assert.Empty(VisitorSessions);
        Assert.Empty(_journal.Entries);
    }

    // The previous group is over (its host launched nothing for 15 minutes): it does not hold the visitor,
    // and its end is recorded at the instant the absence began.
    [Fact]
    public async Task Should_JoinGroup_When_PreviousGroupEndedByHostAbsence()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var previousStepEnd = NowUtc.AddMinutes(-20);
        var previous = GroupSession.Start(Guid.NewGuid(), UserId.New(), NowUtc.AddMinutes(-45), previousStepEnd, Visitor, NowUtc.AddMinutes(-40)).Value;
        _groups.Seed(previous);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(GroupSessionEndReason.HostAbsent, previous.EndReason);
        Assert.Equal(GroupRules.HostAbsentAt(previousStepEnd), previous.EndedAtUtc);
        Assert.True(_groups.Groups.Single(g => g.Id == RootId).IsActiveMember(Visitor));
    }

    [Fact]
    public async Task Should_JoinGroup_When_ConcurrentCreationLost()
    {
        GivenVisitorIsMember();
        var root = GivenTargetIsJoinable();
        var concurrent = GroupSession.Start(RootId, Host, root.StartedAt!.Value, EndsAtUtc, OtherMember, NowUtc).Value;
        _groups.TryAddResult = false;
        _groups.OnReloadAfterRejectedAdd = () => concurrent;

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Same(concurrent, Assert.Single(_groups.Groups));
        Assert.Equal(2, concurrent.ActiveMemberCount);
        Assert.True(concurrent.IsActiveMember(Visitor));
        Assert.Equal(1, _groups.JoinCallCount);
        Assert.Equal(0, _groups.UpdateCallCount);
        Assert.Equal(PomodoroSessionId.From(RootId), Assert.Single(VisitorSessions).SharedSprintId);
    }

    // Decision 2: the membership is written first, then withdrawn when the session cannot exist.
    [Fact]
    public async Task Should_LeaveGroup_When_SessionInsertRejected()
    {
        GivenVisitorIsMember();
        var group = GivenGroupRunning();
        var winner = OwnSprint(NowUtc);
        var memberWhenRejected = false;
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);
        _sessions.OnInsertRejected = () => memberWhenRejected = group.IsActiveMember(Visitor);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.True(memberWhenRejected);
        // IsActiveMember is "LeftAtUtc is null": the departure is recorded.
        Assert.False(group.IsActiveMember(Visitor));
        Assert.Equal(1, group.ActiveMemberCount);
        Assert.Equal(1, _groups.JoinCallCount);
        Assert.Equal(1, _groups.UpdateCallCount); // The departure.
        Assert.Same(winner, Assert.Single(VisitorSessions));
        Assert.Empty(_journal.Entries);
    }

    // A double click whose first request already recorded the membership: the session decides, through
    // the unique index and the idempotency window.
    [Fact]
    public async Task Should_JoinCurrentStep_When_AlreadyActiveMemberWithoutSession()
    {
        GivenVisitorIsMember();
        var root = GivenTargetIsJoinable();
        var group = GroupSession.Start(RootId, Host, root.StartedAt!.Value, EndsAtUtc, Visitor, NowUtc.AddSeconds(-1)).Value;
        _groups.Seed(group);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, group.ActiveMemberCount);
        Assert.Equal(0, _groups.JoinCallCount);
        Assert.Equal(0, _groups.UpdateCallCount);
        Assert.Single(VisitorSessions);
    }

    [Fact]
    public async Task Should_ReturnNotJoinable_When_RootSessionMissing()
    {
        GivenVisitorIsMember();
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(RootId, EndsAtUtc, GroupStepType.Sprint, GroupPhase.Running));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.Empty(_groups.Groups);
        Assert.Empty(VisitorSessions);
    }

    [Fact]
    public async Task Should_ReturnNotJoinable_When_RootSessionNoLongerActive()
    {
        GivenVisitorIsMember();
        var root = GivenTargetIsJoinable();
        Assert.True(root.Interrupt(NowUtc.AddSeconds(-1)).IsSuccess);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.Empty(_groups.Groups);
        Assert.Empty(VisitorSessions);
    }

    [Fact]
    public async Task Should_ReturnNotJoinable_When_GroupClosed()
    {
        GivenVisitorIsMember();
        var group = GivenGroupRunning();
        Assert.True(group.Close().IsSuccess);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.False(group.IsActiveMember(Visitor));
        Assert.Equal(0, _groups.JoinCallCount);
        Assert.Equal(0, _groups.UpdateCallCount);
        Assert.Empty(VisitorSessions);
    }

    [Fact]
    public async Task Should_ReturnNotJoinable_When_GroupStoppedByHost()
    {
        GivenVisitorIsMember();
        var group = GivenGroupRunning();
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, NowUtc.AddSeconds(-1)).IsSuccess);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.False(group.IsActiveMember(Visitor));
        Assert.Empty(VisitorSessions);
    }

    // The host closed the group between the read and the save: the join, checked against the version of the
    // group, is rejected, then decided again on the group as stored.
    [Fact]
    public async Task Should_ReturnNotJoinable_When_HostClosedGroupDuringJoin()
    {
        GivenVisitorIsMember();
        GivenGroupRunning();
        var stored = StoredGroup();
        Assert.True(stored.Close().IsSuccess);
        _groups.TryJoinResults.Enqueue(false);
        _groups.OnReloadAfterRejectedJoin = () => stored;

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.False(stored.IsActiveMember(Visitor));
        Assert.Equal(1, _groups.JoinCallCount);
        Assert.Empty(VisitorSessions);
        Assert.Empty(_journal.Entries);
    }

    // A second tab of the same user recorded the membership first: on the group as stored, the visitor is
    // already a member, and the session insert decides as for a double click.
    [Fact]
    public async Task Should_JoinCurrentStep_When_SameUserRecordedMembershipFirst()
    {
        GivenVisitorIsMember();
        GivenGroupRunning();
        var stored = StoredGroup();
        Assert.True(stored.Join(Visitor, NowUtc).IsSuccess);
        _groups.TryJoinResults.Enqueue(false);
        _groups.OnReloadAfterRejectedJoin = () => stored;

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Same(stored, Assert.Single(_groups.Groups));
        Assert.Equal(2, stored.ActiveMemberCount);
        Assert.Equal(PomodoroSessionId.From(RootId), Assert.Single(VisitorSessions).SharedSprintId);
    }

    // Rejected again on the group read back: the group keeps moving under the request, the card reads its
    // state again.
    [Fact]
    public async Task Should_ReturnNotJoinable_When_JoinRejectedTwice()
    {
        GivenVisitorIsMember();
        GivenGroupRunning();
        _groups.TryJoinResults.Enqueue(false);
        _groups.TryJoinResults.Enqueue(false);
        _groups.OnReloadAfterRejectedJoin = StoredGroup;

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.Equal(2, _groups.JoinCallCount);
        Assert.Empty(VisitorSessions);
        Assert.Empty(_journal.Entries);
    }

    /// <summary>A fresh read of the group of <see cref="GivenGroupRunning"/>, as the store holds it.</summary>
    private GroupSession StoredGroup()
    {
        var root = _sessions.Sessions.Single(s => s.Id.Value == RootId);
        var group = GroupSession.Start(RootId, Host, root.StartedAt!.Value, EndsAtUtc, OtherMember, NowUtc.AddMinutes(-3));
        Assert.True(group.IsSuccess);
        return group.Value;
    }

    /// <summary>Each read of the clock returns a later instant, like a real clock across slow reads.</summary>
    private sealed class SteppingTimeProvider(DateTimeOffset start, TimeSpan step) : TimeProvider
    {
        private DateTimeOffset _next = start;

        public override DateTimeOffset GetUtcNow()
        {
            var current = _next;
            _next += step;
            return current;
        }
    }
}
