using KairuFocus.Application.Community.Commands.LaunchGroupStep;
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

public sealed class LaunchGroupStepCommandHandlerTests
{
    // The first sprint of the group runs from 10:00 to 10:25; default durations 25 / 5 / 15 min.
    private static readonly DateTime StepStart = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StepEnd = StepStart.AddMinutes(25);
    private static readonly DateTime WaitingNow = StepEnd.AddMinutes(1);
    private static readonly Guid GroupId = new("10000000-0000-0000-0000-000000000001");
    private static readonly UserId Host = UserId.From(new Guid("20000000-0000-0000-0000-000000000002"));
    private static readonly UserId Participant = UserId.From(new Guid("30000000-0000-0000-0000-000000000003"));

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakePomodoroSettingsRepository _settings = new();
    private readonly FakeJournalEntryRepository _journal = new();

    private LaunchGroupStepCommandHandler CreateSut(UserId me, DateTime now) =>
        new(
            _profiles,
            _groups,
            _sessions,
            _settings,
            new FakeMediator(_journal),
            new MembershipCurrentUser(me),
            new FixedTimeProvider(new DateTimeOffset(now)),
            NullLogger<LaunchGroupStepCommandHandler>.Instance);

    /// <summary>The group of <see cref="Host"/>, joined by <see cref="Participant"/> during its first sprint.</summary>
    private GroupSession GivenGroup()
    {
        var group = GroupSession.Start(GroupId, Host, StepStart, StepEnd, Participant, StepStart.AddMinutes(1));
        Assert.True(group.IsSuccess);
        _groups.Seed(group.Value);
        return group.Value;
    }

    private List<PomodoroSession> SessionsOf(UserId userId) =>
        _sessions.Sessions.Where(s => s.OwnerId == userId).ToList();

    // --- Nominal -------------------------------------------------------------------------------

    [Fact]
    public async Task Should_LaunchShortBreak_When_Waiting()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("ShortBreak"));

        var breakEnd = WaitingNow.AddMinutes(5);
        Assert.True(result.IsSuccess);
        Assert.False(result.HostAlone);
        Assert.Equal(new DateTimeOffset(breakEnd), result.StepEndsAt);

        var session = Assert.Single(SessionsOf(Host));
        Assert.Equal(PomodoroSessionType.ShortBreak, session.SessionType);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
        Assert.Equal(PomodoroSessionId.From(GroupId), session.SharedSprintId);
        Assert.Equal(breakEnd, session.SharedEndAt);
        Assert.Equal(WaitingNow, session.StartedAt);

        Assert.Equal(2, group.CurrentStep.Number);
        Assert.Equal(GroupStepType.ShortBreak, group.CurrentStep.Type);
        Assert.Equal(WaitingNow, group.CurrentStep.StartedAtUtc);
        Assert.Equal(breakEnd, group.CurrentStep.EndsAtUtc);
        Assert.Equal(1, _groups.UpdateCallCount);

        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.BreakStarted, entry.EventType);
        Assert.Equal(session.Id.Value, entry.ResourceId);
        Assert.Equal(WaitingNow, entry.OccurredAt);
    }

    [Fact]
    public async Task Should_UseLongBreakDuration_When_LongBreakChosen()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("LongBreak"));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(WaitingNow.AddMinutes(15)), result.StepEndsAt);
        var session = Assert.Single(SessionsOf(Host));
        Assert.Equal(PomodoroSessionType.LongBreak, session.SessionType);
        Assert.Equal(WaitingNow.AddMinutes(15), session.SharedEndAt);
        Assert.Equal(GroupStepType.LongBreak, group.CurrentStep.Type);
    }

    [Fact]
    public async Task Should_LaunchSprintAtConfiguredDuration_When_SprintChosen()
    {
        _profiles.Seed(Host, "lea_dev");
        _settings.Settings = PomodoroSettings.Create(50, 10, 20, 4).Value;
        var group = GivenGroup();

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(WaitingNow.AddMinutes(50)), result.StepEndsAt);
        var session = Assert.Single(SessionsOf(Host));
        Assert.Equal(PomodoroSessionType.Sprint, session.SessionType);
        Assert.Equal(50, session.PlannedDurationMinutes);
        Assert.Equal(2, group.SprintCount);
        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintStarted, entry.EventType);
    }

    [Fact]
    public async Task Should_EndGroupAndReportHostAlone_When_NoActiveMember()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();
        Assert.True(group.Leave(Participant, StepStart.AddMinutes(20)).IsSuccess);

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.True(result.IsSuccess);
        Assert.True(result.HostAlone);
        Assert.Null(result.StepEndsAt);
        Assert.Equal(GroupSessionEndReason.HostAlone, group.EndReason);
        Assert.Equal(WaitingNow, group.EndedAtUtc);
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Equal(1, _groups.UpdateCallCount);
        Assert.Empty(SessionsOf(Host));
        Assert.Empty(_journal.Entries);
    }

    // --- Refusals ------------------------------------------------------------------------------

    [Fact]
    public async Task Should_RefuseLaunch_When_NotHost()
    {
        _profiles.Seed(Participant, "tomtom");
        var group = GivenGroup();

        var result = await CreateSut(Participant, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotSprintHost, result.Error);
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_RefuseLaunch_When_NoGroup()
    {
        _profiles.Seed(Host, "lea_dev");

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotSprintHost, result.Error);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_NoProfile()
    {
        GivenGroup();

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_RefuseLaunch_When_StepRunning()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();

        var result = await CreateSut(Host, StepStart.AddMinutes(10)).Handle(new LaunchGroupStepCommand("ShortBreak"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupStepRunning, result.Error);
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Equal(0, _groups.UpdateCallCount);
        Assert.Empty(_sessions.Sessions);
    }

    [Theory]
    [InlineData("Nap")]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("shortbreak")]
    public async Task Should_RefuseLaunch_When_StepTypeIsInvalid(string stepType)
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand(stepType));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.InvalidGroupStep, result.Error);
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_ReportGroupEnded_When_HostAbsentLimitPassed()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();

        var result = await CreateSut(Host, StepEnd.AddMinutes(16)).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupEnded, result.Error);
        Assert.Equal(GroupSessionEndReason.HostAbsent, group.EndReason);
        Assert.Equal(StepEnd.AddMinutes(15), group.EndedAtUtc);
        Assert.Equal(1, _groups.UpdateCallCount);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_ReportGroupEnded_When_GroupAlreadyStopped()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, StepStart.AddMinutes(20)).IsSuccess);

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupEnded, result.Error);
        Assert.Equal(0, _groups.UpdateCallCount);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_RefuseLaunch_When_VersionConflict()
    {
        _profiles.Seed(Host, "lea_dev");
        GivenGroup();
        _groups.TryUpdateResult = false;

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupStepRunning, result.Error);
        Assert.Equal(1, _groups.UpdateCallCount);
        Assert.Empty(_sessions.Sessions);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_RefuseHostAlone_When_VersionConflict()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();
        Assert.True(group.Leave(Participant, StepStart.AddMinutes(20)).IsSuccess);
        _groups.TryUpdateResult = false;

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupStepRunning, result.Error);
        Assert.Empty(_sessions.Sessions);
    }

    // Decision 3: the group is written first, the step stays launched for the participants.
    [Fact]
    public async Task Should_KeepStepAndReportSprintActive_When_HostRunsAnotherSession()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();
        var solo = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Host);
        Assert.True(solo.Start(WaitingNow.AddSeconds(-30)).IsSuccess);
        _sessions.Sessions.Add(solo);

        var result = await CreateSut(Host, WaitingNow).Handle(new LaunchGroupStepCommand("ShortBreak"));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.Equal(2, group.CurrentStep.Number);
        Assert.Same(solo, Assert.Single(SessionsOf(Host)));
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_KeepLaunch_When_JournalFails()
    {
        _profiles.Seed(Host, "lea_dev");
        GivenGroup();
        var mediator = new FakeMediator(_journal) { ThrowOnCreateEntry = true };
        var sut = new LaunchGroupStepCommandHandler(
            _profiles, _groups, _sessions, _settings, mediator, new MembershipCurrentUser(Host),
            new FixedTimeProvider(new DateTimeOffset(WaitingNow)), NullLogger<LaunchGroupStepCommandHandler>.Instance);

        var result = await sut.Handle(new LaunchGroupStepCommand("Sprint"));

        Assert.True(result.IsSuccess);
        Assert.Single(SessionsOf(Host));
    }
}
