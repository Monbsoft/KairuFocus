using KairuFocus.Application.Community.Commands.StartGroupBreak;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class StartGroupBreakCommandHandlerTests
{
    private const string VisitorName = "tomtom";
    private const int ShortBreakMinutes = 5;

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly Guid RootId = new("10000000-0000-0000-0000-000000000001");
    private static readonly UserId Visitor = FakeCurrentUserService.TestUserId;

    // The common sprint ended ten seconds ago: the page asks for the break as its countdown reaches zero.
    private static readonly DateTime SprintEnd = NowUtc.AddSeconds(-10);

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakeJournalEntryRepository _journal = new();
    private readonly FakeMediator _mediator;

    public StartGroupBreakCommandHandlerTests()
    {
        _mediator = new FakeMediator(_journal);
    }

    private StartGroupBreakCommandHandler CreateSut() =>
        new(
            _profiles,
            _openSprints,
            _sessions,
            _mediator,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now),
            NullLogger<StartGroupBreakCommandHandler>.Instance);

    private Task<StartGroupBreakResult> HandleAsync() => CreateSut().Handle(new StartGroupBreakCommand());

    private void GivenVisitorIsMember() => _profiles.Seed(Visitor, VisitorName);

    private void GivenGroupTakesABreak(Guid? rootId = null, int shortBreakMinutes = ShortBreakMinutes) =>
        _openSprints.SeedGroupBreak(rootId ?? RootId, new GroupBreakRules(shortBreakMinutes));

    /// <summary>A sprint that the visitor joined and that ended at <paramref name="sharedEnd"/>.</summary>
    private static PomodoroSession CompletedJoinedSprint(DateTime sharedEnd)
    {
        var startedAt = sharedEnd.AddMinutes(-10);
        var joined = PomodoroSession.CreateJoined(Visitor, PomodoroSessionId.From(RootId), sharedEnd, startedAt);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(startedAt).IsSuccess);
        Assert.True(joined.Value.Complete(sharedEnd).IsSuccess);
        return joined.Value;
    }

    /// <summary>A sprint that the visitor hosts, completed at <paramref name="endedAt"/>.</summary>
    private static PomodoroSession CompletedOwnSprint(DateTime endedAt, int plannedMinutes = 25)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedMinutes, Visitor);
        Assert.True(session.Start(endedAt.AddMinutes(-plannedMinutes)).IsSuccess);
        Assert.True(session.Complete(endedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession ActiveGroupBreak(DateTime startedAt, DateTime sharedEnd)
    {
        var created = PomodoroSession.CreateJoinedBreak(Visitor, PomodoroSessionId.From(RootId), sharedEnd, startedAt);
        Assert.True(created.IsSuccess);
        Assert.True(created.Value.Start(startedAt).IsSuccess);
        return created.Value;
    }

    private static PomodoroSession ActiveOwnSession(PomodoroSessionType type, DateTime startedAt)
    {
        var session = PomodoroSession.Create(type, type == PomodoroSessionType.Sprint ? 25 : 5, Visitor);
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    // --- Nominal -------------------------------------------------------------------------------

    [Fact]
    public async Task Should_StartBreakOfTheGroup_When_JoinedSprintJustEnded()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));
        var expectedEnd = SprintEnd.AddMinutes(ShortBreakMinutes);

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(new DateTimeOffset(expectedEnd, TimeSpan.Zero), result.SharedEndAt);

        var session = _sessions.Sessions.Single(s => s.Status == PomodoroSessionStatus.Active);
        Assert.Equal(Visitor, session.OwnerId);
        Assert.Equal(PomodoroSessionType.ShortBreak, session.SessionType);
        Assert.Equal(PomodoroSessionId.From(RootId), session.SharedSprintId);
        Assert.Equal(expectedEnd, session.SharedEndAt);
        Assert.Equal(NowUtc, session.StartedAt);
        Assert.Equal(5, session.PlannedDurationMinutes); // 4:50 rounded up.

        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.BreakStarted, entry.EventType);
        Assert.Equal(session.Id.Value, entry.ResourceId);
        Assert.Equal(NowUtc, entry.OccurredAt);
        Assert.Equal(Visitor, entry.OwnerId);
    }

    // The host's session is the root of the group: nobody joined it.
    [Fact]
    public async Task Should_StartBreakOnOwnRoot_When_VisitorHostedTheSprint()
    {
        GivenVisitorIsMember();
        var hosted = CompletedOwnSprint(SprintEnd);
        _sessions.Sessions.Add(hosted);
        GivenGroupTakesABreak(hosted.Id.Value);

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(SprintEnd.AddMinutes(ShortBreakMinutes), TimeSpan.Zero), result.SharedEndAt);
        var session = _sessions.Sessions.Single(s => s.Status == PomodoroSessionStatus.Active);
        Assert.Equal(hosted.Id, session.SharedSprintId);
    }

    [Fact]
    public async Task Should_UseBreakLengthOfTheHost_When_RulesSayTen()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak(shortBreakMinutes: 10);
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));

        var result = await HandleAsync();

        Assert.Equal(new DateTimeOffset(SprintEnd.AddMinutes(10), TimeSpan.Zero), result.SharedEndAt);
        Assert.Equal(10, _sessions.Sessions.Single(s => s.Status == PomodoroSessionStatus.Active).PlannedDurationMinutes);
    }

    // Page closed at the end of the sprint: coming back joins the break for the time that is left.
    [Fact]
    public async Task Should_StartBreakForRemainingTime_When_ReturningDuringTheBreak()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        var endedThreeMinutesAgo = NowUtc.AddMinutes(-3);
        _sessions.Sessions.Add(CompletedJoinedSprint(endedThreeMinutesAgo));

        var result = await HandleAsync();

        Assert.Equal(new DateTimeOffset(endedThreeMinutesAgo.AddMinutes(ShortBreakMinutes), TimeSpan.Zero), result.SharedEndAt);
        var session = _sessions.Sessions.Single(s => s.Status == PomodoroSessionStatus.Active);
        Assert.Equal(2, session.PlannedDurationMinutes);
        Assert.Equal(NowUtc, session.StartedAt);
    }

    [Theory]
    [InlineData(5)]  // The break ends exactly now.
    [InlineData(6)]
    [InlineData(120)]
    public async Task Should_StartNoBreak_When_ReturningAfterTheBreakEnded(int minutesSinceSprintEnd)
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(NowUtc.AddMinutes(-minutesSinceSprintEnd)));

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    // --- Nothing applies -----------------------------------------------------------------------

    [Fact]
    public async Task Should_StartNoBreak_When_VisitorHasNoProfile()
    {
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    // A visitor with no profile is not asked anything else: not even the active session.
    [Fact]
    public async Task Should_StartNoBreak_When_VisitorHasNoProfileAndAnActiveSession()
    {
        _sessions.Sessions.Add(ActiveOwnSession(PomodoroSessionType.Sprint, NowUtc.AddMinutes(-3)));

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.SharedEndAt);
    }

    [Fact]
    public async Task Should_StartNoBreak_When_VisitorHasNoSession()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    // Fewer than two participants, or an unknown root: the read finds no group.
    [Fact]
    public async Task Should_StartNoBreak_When_GroupHasFewerThanTwoParticipants()
    {
        GivenVisitorIsMember();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    [Fact]
    public async Task Should_StartNoBreak_When_SprintWasSolo()
    {
        GivenVisitorIsMember();
        _sessions.Sessions.Add(CompletedOwnSprint(SprintEnd));

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    [Fact]
    public async Task Should_StartNoBreak_When_SprintWasInterrupted()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        var interrupted = ActiveOwnSession(PomodoroSessionType.Sprint, NowUtc.AddMinutes(-20));
        Assert.True(interrupted.Interrupt(SprintEnd).IsSuccess);
        _sessions.Sessions.Add(interrupted);

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    // The latest session decides: an interrupted sprint after a completed one leaves no break.
    [Fact]
    public async Task Should_StartNoBreak_When_LatestSprintWasInterruptedAfterACompletedOne()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(NowUtc.AddMinutes(-40)));
        var interrupted = ActiveOwnSession(PomodoroSessionType.Sprint, NowUtc.AddMinutes(-20));
        Assert.True(interrupted.Interrupt(SprintEnd).IsSuccess);
        _sessions.Sessions.Add(interrupted);

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    [Fact]
    public async Task Should_StartNoBreak_When_GroupBreakWasInterrupted()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(NowUtc.AddMinutes(-3)));
        var groupBreak = ActiveGroupBreak(NowUtc.AddMinutes(-3), NowUtc.AddMinutes(2));
        Assert.True(groupBreak.Interrupt(NowUtc.AddMinutes(-1)).IsSuccess);
        _sessions.Sessions.Add(groupBreak);

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    [Fact]
    public async Task Should_StartNoBreak_When_GroupBreakWasCompleted()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd.AddMinutes(-5)));
        var groupBreak = ActiveGroupBreak(SprintEnd.AddMinutes(-5), SprintEnd);
        Assert.True(groupBreak.Complete(SprintEnd).IsSuccess);
        _sessions.Sessions.Add(groupBreak);

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    [Theory]
    [InlineData(PomodoroSessionType.ShortBreak)]
    [InlineData(PomodoroSessionType.LongBreak)]
    public async Task Should_StartNoBreak_When_LatestSessionIsABreakOfTheVisitor(PomodoroSessionType type)
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        var own = ActiveOwnSession(type, NowUtc.AddMinutes(-6));
        Assert.True(own.Complete(SprintEnd).IsSuccess);
        _sessions.Sessions.Add(own);

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    [Fact]
    public async Task Should_StartNoBreak_When_LatestSprintIsFree()
    {
        GivenVisitorIsMember();
        var free = CompletedOwnSprint(SprintEnd, plannedMinutes: 0);
        _sessions.Sessions.Add(free);
        GivenGroupTakesABreak(free.Id.Value);

        var result = await HandleAsync();

        AssertNoBreak(result);
    }

    // --- Active session ------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnActiveBreakAsIs_When_GroupBreakIsAlreadyRunning()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        var sharedEnd = NowUtc.AddMinutes(2);
        var running = ActiveGroupBreak(NowUtc.AddSeconds(-2), sharedEnd);
        _sessions.Sessions.Add(running);

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(sharedEnd, TimeSpan.Zero), result.SharedEndAt);
        Assert.Same(running, Assert.Single(_sessions.Sessions));
        Assert.Empty(_journal.Entries);
    }

    // A second tab opened long after the first request: no idempotency window, the break is returned.
    [Fact]
    public async Task Should_ReturnActiveBreak_When_ItStartedLongAgo()
    {
        GivenVisitorIsMember();
        var sharedEnd = NowUtc.AddMinutes(1);
        _sessions.Sessions.Add(ActiveGroupBreak(NowUtc.AddMinutes(-4), sharedEnd));

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(sharedEnd, TimeSpan.Zero), result.SharedEndAt);
        Assert.Single(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_ReturnSameBreak_When_RequestedTwice()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));

        var first = await HandleAsync();
        var second = await HandleAsync();

        Assert.True(first.IsSuccess);
        Assert.Equal(first.SharedEndAt, second.SharedEndAt);
        Assert.Single(_sessions.Sessions, s => s.Status == PomodoroSessionStatus.Active);
        Assert.Single(_journal.Entries);
    }

    [Theory]
    [InlineData(PomodoroSessionType.Sprint)]
    [InlineData(PomodoroSessionType.ShortBreak)] // The visitor's own break: not shared with a group.
    [InlineData(PomodoroSessionType.LongBreak)]
    public async Task Should_ReturnAlreadyActive_When_VisitorRunsAnotherSession(PomodoroSessionType type)
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        var other = ActiveOwnSession(type, NowUtc.AddSeconds(-30));
        _sessions.Sessions.Add(other);

        var result = await HandleAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.Null(result.SharedEndAt);
        Assert.Same(other, Assert.Single(_sessions.Sessions));
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorJoinedAnotherSprint()
    {
        GivenVisitorIsMember();
        var joined = PomodoroSession.CreateJoined(
            Visitor, PomodoroSessionId.New(), NowUtc.AddMinutes(10), NowUtc.AddMinutes(-1)).Value;
        Assert.True(joined.Start(NowUtc.AddMinutes(-1)).IsSuccess);
        _sessions.Sessions.Add(joined);

        var result = await HandleAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // --- Concurrent insert ---------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnWinningBreak_When_InsertRejectedForAGroupBreak()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));
        var winnerEnd = SprintEnd.AddMinutes(ShortBreakMinutes);
        var winner = ActiveGroupBreak(NowUtc.AddSeconds(-1), winnerEnd);
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(winnerEnd, TimeSpan.Zero), result.SharedEndAt);
        Assert.Single(_sessions.Sessions, s => s.Status == PomodoroSessionStatus.Active);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_InsertRejectedForAnotherSession()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));
        var winner = ActiveOwnSession(PomodoroSessionType.Sprint, NowUtc.AddSeconds(-1));
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);

        var result = await HandleAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnConcurrentStart_When_InsertRejectedAndNothingReadable()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));
        var winner = ActiveOwnSession(PomodoroSessionType.Sprint, NowUtc);
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);
        _sessions.OnInsertRejected = () => Assert.True(winner.Interrupt(NowUtc).IsSuccess);

        var result = await HandleAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.ConcurrentSessionStart, result.Error);
        Assert.Empty(_journal.Entries);
    }

    // --- Journal -------------------------------------------------------------------------------

    // ADR-023: the session is persisted, a failing journal never fails the break.
    [Fact]
    public async Task Should_Succeed_When_JournalFails()
    {
        GivenVisitorIsMember();
        GivenGroupTakesABreak();
        _sessions.Sessions.Add(CompletedJoinedSprint(SprintEnd));
        _mediator.ThrowOnCreateEntry = true;

        var result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.SharedEndAt);
        Assert.Single(_sessions.Sessions, s => s.SessionType == PomodoroSessionType.ShortBreak);
        Assert.Empty(_journal.Entries);
    }

    private void AssertNoBreak(StartGroupBreakResult result)
    {
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Null(result.SharedEndAt);
        Assert.DoesNotContain(_sessions.Sessions, s => s.Status == PomodoroSessionStatus.Active);
        Assert.Empty(_journal.Entries);
    }
}
