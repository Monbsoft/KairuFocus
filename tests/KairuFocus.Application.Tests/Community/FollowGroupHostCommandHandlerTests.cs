using KairuFocus.Application.Community.Commands.FollowGroupHost;
using KairuFocus.Application.Gamification.Commands.CreditSprintXp;
using KairuFocus.Application.Pomodoro.Commands.CompleteSession;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Gamification;
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

public sealed class FollowGroupHostCommandHandlerTests
{
    // The step the host launched runs from 10:00 to 10:25 (the first sprint of the group ran 09:30 - 09:55).
    private static readonly DateTime StepStart = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StepEnd = StepStart.AddMinutes(25);
    private static readonly Guid GroupId = new("10000000-0000-0000-0000-000000000001");
    private static readonly PomodoroSessionId RootId = PomodoroSessionId.From(GroupId);
    private static readonly UserId Host = UserId.From(new Guid("20000000-0000-0000-0000-000000000002"));
    private static readonly UserId Participant = UserId.From(new Guid("30000000-0000-0000-0000-000000000003"));

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakePomodoroSettingsRepository _settings = new();
    private readonly FakeJournalEntryRepository _journal = new();
    private readonly FakeXpGainRepository _xpGains = new();

    public FollowGroupHostCommandHandlerTests()
    {
        _profiles.Seed(Participant, "tomtom");
    }

    /// <summary>The handler as <paramref name="me"/>, at <paramref name="now"/>. Completing a session goes through
    /// the real <see cref="CompleteSessionCommandHandler"/> and XP credit.</summary>
    private FollowGroupHostCommandHandler CreateSut(DateTime now, UserId? me = null)
    {
        var currentUser = new MembershipCurrentUser(me ?? Participant);
        var credit = new CreditSprintXpCommandHandler(_xpGains, _sessions, NullLogger<CreditSprintXpCommandHandler>.Instance);
        var complete = new CompleteSessionCommandHandler(
            _sessions,
            _settings,
            new FakeMediator(_journal, credit),
            currentUser,
            NullLogger<CompleteSessionCommandHandler>.Instance);

        return new FollowGroupHostCommandHandler(
            _profiles,
            _groups,
            _sessions,
            new FakeMediator(_journal, completeSession: complete),
            currentUser,
            new FixedTimeProvider(new DateTimeOffset(now)),
            NullLogger<FollowGroupHostCommandHandler>.Instance);
    }

    /// <summary>The group of <see cref="Host"/>, joined by <see cref="Participant"/> in its first sprint
    /// (09:30 - 09:55), in which the host then launched a step of <paramref name="type"/> from 10:00 to
    /// <paramref name="end"/> (default 10:25).</summary>
    private GroupSession GivenGroupInStep(GroupStepType type = GroupStepType.Sprint, DateTime? end = null)
    {
        var started = GroupSession.Start(
            GroupId, Host, StepStart.AddMinutes(-30), StepStart.AddMinutes(-5), Participant, StepStart.AddMinutes(-29));
        Assert.True(started.IsSuccess);
        var group = started.Value;
        Assert.True(group.LaunchStep(type, StepStart, end ?? StepEnd, StepStart).IsSuccess);
        _groups.Seed(group);
        return group;
    }

    /// <summary>The participant's session of a step of the group, started at <paramref name="startedAt"/>.</summary>
    private PomodoroSession GivenParticipantStep(PomodoroSessionType type, DateTime startedAt, DateTime sharedEndAt)
    {
        var joined = PomodoroSession.CreateJoinedStep(type, Participant, RootId, sharedEndAt, startedAt);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(startedAt).IsSuccess);
        _sessions.Sessions.Add(joined.Value);
        return joined.Value;
    }

    private List<PomodoroSession> ParticipantSessions =>
        _sessions.Sessions.Where(s => s.OwnerId == Participant).ToList();

    // --- Running step --------------------------------------------------------------------------

    [Fact]
    public async Task Should_CreateSameStep_When_HostLaunched()
    {
        GivenGroupInStep();
        var now = StepStart.AddSeconds(3);

        var result = await CreateSut(now).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(FollowGroupHostResult.NewStepOutcome, result.Outcome);
        Assert.Equal("Sprint", result.StepType);
        Assert.Equal(new DateTimeOffset(StepEnd), result.StepEndsAt);
        Assert.Equal(0, result.XpAwarded);

        var session = Assert.Single(ParticipantSessions);
        Assert.Equal(PomodoroSessionType.Sprint, session.SessionType);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
        Assert.Equal(RootId, session.SharedSprintId);
        Assert.Equal(StepEnd, session.SharedEndAt);
        Assert.Equal(now, session.StartedAt);

        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintStarted, entry.EventType);
        Assert.Equal(session.Id.Value, entry.ResourceId);
    }

    [Fact]
    public async Task Should_CreateBreak_When_HostLaunchedBreak()
    {
        GivenGroupInStep(GroupStepType.LongBreak, StepStart.AddMinutes(15));

        var result = await CreateSut(StepStart.AddSeconds(2)).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.NewStepOutcome, result.Outcome);
        Assert.Equal("LongBreak", result.StepType);
        var session = Assert.Single(ParticipantSessions);
        Assert.Equal(PomodoroSessionType.LongBreak, session.SessionType);
        Assert.Equal(StepStart.AddMinutes(15), session.SharedEndAt);
        Assert.Equal(JournalEventType.BreakStarted, Assert.Single(_journal.Entries).EventType);
    }

    [Fact]
    public async Task Should_ReturnSameStep_When_CalledTwice()
    {
        GivenGroupInStep();

        await CreateSut(StepStart.AddSeconds(3)).Handle(new FollowGroupHostCommand());
        var second = await CreateSut(StepStart.AddSeconds(8)).Handle(new FollowGroupHostCommand());

        Assert.True(second.IsSuccess);
        Assert.Equal(FollowGroupHostResult.SameStepOutcome, second.Outcome);
        Assert.Equal("Sprint", second.StepType);
        Assert.Equal(new DateTimeOffset(StepEnd), second.StepEndsAt);
        Assert.Single(ParticipantSessions);
        Assert.Single(_journal.Entries);
    }

    [Fact]
    public async Task Should_ResumeCurrentStep_When_ParticipantReturnsLate()
    {
        GivenGroupInStep();
        var now = StepStart.AddMinutes(20);

        var result = await CreateSut(now).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.NewStepOutcome, result.Outcome);
        var session = Assert.Single(ParticipantSessions);
        Assert.Equal(5, session.PlannedDurationMinutes);
        Assert.Equal(StepEnd, session.SharedEndAt);
        Assert.Equal(now, session.StartedAt);
    }

    [Fact]
    public async Task Should_ClosePreviousStepFirst_When_ItIsStillActive()
    {
        GivenGroupInStep();
        var previous = GivenParticipantStep(PomodoroSessionType.Sprint, StepStart.AddMinutes(-29), StepStart.AddMinutes(-5));

        var result = await CreateSut(StepStart.AddSeconds(3)).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.NewStepOutcome, result.Outcome);
        Assert.Equal(PomodoroSessionStatus.Completed, previous.Status);
        Assert.Equal(StepStart.AddMinutes(-5), previous.EndedAt);
        Assert.Equal(10, result.XpAwarded);
        var current = Assert.Single(ParticipantSessions, s => s.Status == PomodoroSessionStatus.Active);
        Assert.Equal(StepEnd, current.SharedEndAt);
        Assert.Equal(
            [JournalEventType.SprintCompleted, JournalEventType.SprintStarted],
            _journal.Entries.Select(e => e.EventType).ToArray());
    }

    [Fact]
    public async Task Should_ReturnSameStep_When_ConcurrentRequestCreatedTheStep()
    {
        GivenGroupInStep();
        var now = StepStart.AddSeconds(3);
        _sessions.OnTryAdd = () =>
        {
            _sessions.OnTryAdd = null;
            GivenParticipantStep(PomodoroSessionType.Sprint, now, StepEnd);
        };

        var result = await CreateSut(now).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowGroupHostResult.SameStepOutcome, result.Outcome);
        Assert.Single(ParticipantSessions);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReportSessionAlreadyActive_When_ConcurrentSoloSessionWon()
    {
        GivenGroupInStep();
        var now = StepStart.AddSeconds(3);
        _sessions.OnTryAdd = () =>
        {
            _sessions.OnTryAdd = null;
            var solo = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Participant);
            Assert.True(solo.Start(now).IsSuccess);
            _sessions.Sessions.Add(solo);
        };

        var result = await CreateSut(now).Handle(new FollowGroupHostCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.Null(result.Outcome);
    }

    // --- Waiting -------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Wait_When_HostHasNotLaunched()
    {
        GivenGroupInStep();

        var result = await CreateSut(StepEnd.AddMinutes(2)).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowGroupHostResult.WaitingOutcome, result.Outcome);
        Assert.Equal(new DateTimeOffset(StepEnd.AddMinutes(15)), result.WaitLimitAt);
        Assert.Equal(new DateTimeOffset(StepEnd), result.StepEndsAt);
        Assert.Equal("Sprint", result.StepType);
        Assert.Empty(ParticipantSessions);
        Assert.Equal(0, _groups.UpdateCallCount);
    }

    // --- Ended ---------------------------------------------------------------------------------

    [Fact]
    public async Task Should_CloseSprintAtStopTime_When_HostStopped()
    {
        var group = GivenGroupInStep();
        var sprint = GivenParticipantStep(PomodoroSessionType.Sprint, StepStart, StepEnd);
        var stoppedAt = StepStart.AddMinutes(24);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, stoppedAt).IsSuccess);

        var result = await CreateSut(stoppedAt.AddSeconds(4)).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowGroupHostResult.EndedOutcome, result.Outcome);
        Assert.Equal("Stopped", result.EndReason);
        Assert.Equal(10, result.XpAwarded);
        Assert.Equal(PomodoroSessionStatus.Completed, sprint.Status);
        Assert.Equal(stoppedAt, sprint.EndedAt);
        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintCompleted, entry.EventType);
        Assert.Equal(stoppedAt, entry.OccurredAt);
    }

    [Fact]
    public async Task Should_AwardNoXp_When_StoppedAfter3Minutes()
    {
        var group = GivenGroupInStep();
        var sprint = GivenParticipantStep(PomodoroSessionType.Sprint, StepStart, StepEnd);
        var stoppedAt = StepStart.AddMinutes(3);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, stoppedAt).IsSuccess);

        var result = await CreateSut(stoppedAt.AddSeconds(4)).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.EndedOutcome, result.Outcome);
        Assert.Equal(0, result.XpAwarded);
        Assert.Equal(PomodoroSessionStatus.Completed, sprint.Status);
        Assert.Equal(stoppedAt, sprint.EndedAt);
        Assert.Empty(_xpGains.Gains);
    }

    [Fact]
    public async Task Should_CloseBreakWithoutXp_When_StoppedDuringBreak()
    {
        var breakEnd = StepStart.AddMinutes(15);
        var group = GivenGroupInStep(GroupStepType.LongBreak, breakEnd);
        var longBreak = GivenParticipantStep(PomodoroSessionType.LongBreak, StepStart, breakEnd);
        var stoppedAt = StepStart.AddMinutes(12);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, stoppedAt).IsSuccess);

        var result = await CreateSut(stoppedAt.AddSeconds(4)).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.EndedOutcome, result.Outcome);
        Assert.Equal(0, result.XpAwarded);
        Assert.Equal(PomodoroSessionStatus.Completed, longBreak.Status);
        Assert.Equal(stoppedAt, longBreak.EndedAt);
        Assert.Equal(JournalEventType.BreakCompleted, Assert.Single(_journal.Entries).EventType);
        Assert.Empty(_xpGains.Gains);
    }

    [Fact]
    public async Task Should_CloseSessionAtStepEnd_When_HostAbsentLimitPassed()
    {
        var group = GivenGroupInStep();
        var sprint = GivenParticipantStep(PomodoroSessionType.Sprint, StepStart, StepEnd);

        var result = await CreateSut(StepEnd.AddMinutes(16)).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.EndedOutcome, result.Outcome);
        Assert.Equal("HostAbsent", result.EndReason);
        Assert.Equal(GroupSessionEndReason.HostAbsent, group.EndReason);
        Assert.Equal(1, _groups.UpdateCallCount);
        Assert.Equal(StepEnd, sprint.EndedAt);
        Assert.Equal(10, result.XpAwarded);
    }

    [Fact]
    public async Task Should_ReportEndedWithoutClosing_When_NoSessionOfTheGroupIsActive()
    {
        var group = GivenGroupInStep();
        Assert.True(group.Stop(GroupSessionEndReason.HostLeftCommunity, StepStart.AddMinutes(10)).IsSuccess);

        var result = await CreateSut(StepStart.AddMinutes(11)).Handle(new FollowGroupHostCommand());

        Assert.Equal(FollowGroupHostResult.EndedOutcome, result.Outcome);
        Assert.Equal("HostLeftCommunity", result.EndReason);
        Assert.Equal(0, result.XpAwarded);
        Assert.Empty(_journal.Entries);
    }

    // --- Not in the group ----------------------------------------------------------------------

    [Fact]
    public async Task Should_RecordDeparture_When_ParticipantRunsSoloSession()
    {
        var group = GivenGroupInStep();
        var solo = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Participant);
        Assert.True(solo.Start(StepEnd.AddMinutes(1)).IsSuccess);
        _sessions.Sessions.Add(solo);

        var result = await CreateSut(StepEnd.AddMinutes(2)).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowGroupHostResult.NotInGroupOutcome, result.Outcome);
        Assert.False(group.IsActiveMember(Participant));
        Assert.Null(group.EndedAtUtc);
        Assert.Equal(1, _groups.UpdateCallCount);
        Assert.Equal(PomodoroSessionStatus.Active, solo.Status);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReportNotInGroup_When_CallerIsHost()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroupInStep();

        var result = await CreateSut(StepStart.AddSeconds(3), Host).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowGroupHostResult.NotInGroupOutcome, result.Outcome);
        Assert.Empty(_sessions.Sessions);
        Assert.Null(group.EndedAtUtc);
        Assert.Equal(0, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_ReportNotInGroup_When_NoGroup()
    {
        var result = await CreateSut(StepStart).Handle(new FollowGroupHostCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowGroupHostResult.NotInGroupOutcome, result.Outcome);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_NoProfile()
    {
        GivenGroupInStep();

        var result = await CreateSut(StepStart, Host).Handle(new FollowGroupHostCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Null(result.Outcome);
    }
}
