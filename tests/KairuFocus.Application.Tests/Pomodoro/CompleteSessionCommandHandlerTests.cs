using KairuFocus.Application.Gamification.Commands.CreditSprintXp;
using KairuFocus.Application.Pomodoro.Commands.CompleteSession;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Gamification;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class CompleteSessionCommandHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly FakeJournalEntryRepository _journalRepository = new();
    private readonly FakeXpGainRepository _xpGainRepository = new();
    private readonly FakeMediator _fakeMediator;
    private readonly CompleteSessionCommandHandler _sut;

    public CompleteSessionCommandHandlerTests()
    {
        var creditSprintXpHandler = new CreditSprintXpCommandHandler(
            _xpGainRepository,
            _sessionRepository,
            NullLogger<CreditSprintXpCommandHandler>.Instance);
        _fakeMediator = new FakeMediator(_journalRepository, creditSprintXpHandler);
        _sut = new CompleteSessionCommandHandler(
            _sessionRepository,
            _settingsRepository,
            _fakeMediator,
            new FakeCurrentUserService(),
            NullLogger<CompleteSessionCommandHandler>.Instance);
    }

    private PomodoroSession AddActiveSession(
        PomodoroSessionType type,
        DateTime? startedAt = null,
        int plannedDurationMinutes = 25)
    {
        var session = PomodoroSession.Create(type, plannedDurationMinutes, FakeCurrentUserService.TestUserId);
        session.Start(startedAt ?? DateTime.UtcNow);
        _sessionRepository.Sessions.Add(session);
        return session;
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NoActiveSession()
    {
        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.NoActiveSession, result.Error);
    }

    [Fact]
    public async Task Should_LogSprintCompleted_When_SprintSessionCompleted()
    {
        AddActiveSession(PomodoroSessionType.Sprint);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.SprintCompleted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_LogBreakCompleted_When_ShortBreakSessionCompleted()
    {
        AddActiveSession(PomodoroSessionType.ShortBreak);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.BreakCompleted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_SetSequence1_When_FirstBreakOfTheDay()
    {
        AddActiveSession(PomodoroSessionType.ShortBreak);

        await _sut.Handle(new CompleteSessionCommand());

        Assert.Equal(1, _journalRepository.Entries[0].Sequence);
    }

    [Fact]
    public async Task Should_SetSequence2_When_SecondBreakOfTheDay()
    {
        // première pause déjà enregistrée
        _journalRepository.Entries.Add(
            JournalEntry.Create(
                JournalEventType.BreakCompleted,
                Guid.NewGuid(),
                DateTime.UtcNow.AddHours(-1),
                FakeCurrentUserService.TestUserId,
                1));

        AddActiveSession(PomodoroSessionType.ShortBreak);

        await _sut.Handle(new CompleteSessionCommand());

        var breakEntry = _journalRepository.Entries.Last();
        Assert.Equal(2, breakEntry.Sequence);
    }

    [Fact]
    public async Task Should_NotSetSequence_When_SprintCompleted()
    {
        AddActiveSession(PomodoroSessionType.Sprint);

        await _sut.Handle(new CompleteSessionCommand());

        Assert.Null(_journalRepository.Entries[0].Sequence);
    }

    [Fact]
    public async Task Should_LogBreakCompleted_When_LongBreakSessionCompleted()
    {
        AddActiveSession(PomodoroSessionType.LongBreak);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.BreakCompleted, _journalRepository.Entries[0].EventType);
    }

    // ── Journal failure (ADR-023) ──────────────────────────────────────────

    [Fact]
    public async Task Should_CompleteSession_When_JournalEntryThrows()
    {
        // The completion is already persisted: a failing journal must NEVER fail it.
        AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-25));
        _fakeMediator.ThrowOnCreateEntry = true;

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _sessionRepository.UpdateAsyncCallCount);
        Assert.Empty(_journalRepository.Entries);
    }

    [Fact]
    public async Task Should_CreditXp_When_JournalEntryThrows()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-25));
        _fakeMediator.ThrowOnCreateEntry = true;

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.XpAwarded);
        var gain = Assert.Single(_xpGainRepository.Gains);
        Assert.Equal(session.Id.Value, gain.SessionId);
    }

    // ── XP credit (ADR-023) ────────────────────────────────────────────────

    [Fact]
    public async Task Should_CompleteSession_When_XpCreditThrows()
    {
        // Key rule from the product framing: a failing XP credit must NEVER
        // fail the session completion (log-only).
        AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-25));
        _fakeMediator.ThrowOnCreditSprintXp = true;

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.XpAwarded);
        Assert.Equal(1, _sessionRepository.UpdateAsyncCallCount);
        Assert.Empty(_xpGainRepository.Gains);
    }

    [Fact]
    public async Task Should_CompleteSession_When_XpCreditFails()
    {
        // Non-throwing failure branch (e.g. SessionNotFound): the completion
        // must still succeed, with no XP awarded (log-only error handling).
        AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-25));
        _fakeMediator.CreditSprintXpFailureError = "Session not found for XP credit.";

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.XpAwarded);
        Assert.Equal(1, _sessionRepository.UpdateAsyncCallCount);
        Assert.Empty(_xpGainRepository.Gains);
    }

    [Fact]
    public async Task Should_ReturnXpAwarded_When_SprintEligible()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-25));

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.XpAwarded);
        var gain = Assert.Single(_xpGainRepository.Gains);
        Assert.Equal(session.Id.Value, gain.SessionId);
    }

    [Fact]
    public async Task Should_ReturnZeroXpAwarded_When_BreakCompleted()
    {
        AddActiveSession(PomodoroSessionType.ShortBreak, DateTime.UtcNow.AddMinutes(-25));

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.XpAwarded);
        Assert.Empty(_xpGainRepository.Gains);
    }

    [Fact]
    public async Task Should_ReturnZeroXpAwarded_When_SprintUnder5Minutes()
    {
        AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-4));

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.XpAwarded);
        Assert.Empty(_xpGainRepository.Gains);
    }

    // ── Bounded end time (ADR-025) ─────────────────────────────────────────
    // Sessions started at a fixed past date: a late completion is retained at the scheduled
    // end, so the expected values do not depend on the clock.

    private static readonly DateTime PastStart = new(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_SetEndedAtToScheduledEnd_When_SprintCompletedLate()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, PastStart);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Completed, session.Status);
        Assert.Equal(PastStart.AddMinutes(25), session.EndedAt);
    }

    [Fact]
    public async Task Should_DateJournalEntryAtEndedAt_When_SessionCompletedLate()
    {
        AddActiveSession(PomodoroSessionType.Sprint, PastStart);

        await _sut.Handle(new CompleteSessionCommand());

        var entry = Assert.Single(_journalRepository.Entries);
        Assert.Equal(PastStart.AddMinutes(25), entry.OccurredAt);
    }

    [Fact]
    public async Task Should_DateJournalEntryAtEndedAt_When_SessionCompletedBeforeScheduledEnd()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddMinutes(-10));

        await _sut.Handle(new CompleteSessionCommand());

        var entry = Assert.Single(_journalRepository.Entries);
        Assert.Equal(session.EndedAt, entry.OccurredAt);
    }

    [Fact]
    public async Task Should_Credit10XpDatedAtScheduledEnd_When_SprintCompletedLate()
    {
        AddActiveSession(PomodoroSessionType.Sprint, PastStart);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.Equal(10, result.XpAwarded);
        var gain = Assert.Single(_xpGainRepository.Gains);
        Assert.Equal(PastStart.AddMinutes(25), gain.EarnedAtUtc);
    }

    [Fact]
    public async Task Should_ReturnZeroXpAwarded_When_OneMinuteSprintCompletedLate()
    {
        AddActiveSession(PomodoroSessionType.Sprint, PastStart, plannedDurationMinutes: 1);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.XpAwarded);
        Assert.Empty(_xpGainRepository.Gains);
    }

    [Fact]
    public async Task Should_Credit10Xp_When_FiveMinuteSprintCompletedLate()
    {
        AddActiveSession(PomodoroSessionType.Sprint, PastStart, plannedDurationMinutes: 5);

        var result = await _sut.Handle(new CompleteSessionCommand());

        Assert.Equal(10, result.XpAwarded);
        Assert.Single(_xpGainRepository.Gains);
    }

    [Fact]
    public async Task Should_DateXpGainOnScheduledEndDay_When_CompletionReportedOnLaterDay()
    {
        // Scheduled end 23:50 on day J, completion reported days later: the gain belongs to day J.
        var start = new DateTime(2026, 3, 10, 23, 25, 0, DateTimeKind.Utc);
        AddActiveSession(PomodoroSessionType.Sprint, start);

        await _sut.Handle(new CompleteSessionCommand());

        var gain = Assert.Single(_xpGainRepository.Gains);
        Assert.Equal(new DateTime(2026, 3, 10, 23, 50, 0, DateTimeKind.Utc), gain.EarnedAtUtc);
    }

    [Fact]
    public async Task Should_Credit10XpWithUnboundedEndedAt_When_FreeSprintCompleted()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddHours(-3), plannedDurationMinutes: 0);

        var before = DateTime.UtcNow;
        var result = await _sut.Handle(new CompleteSessionCommand());
        var after = DateTime.UtcNow;

        Assert.Equal(10, result.XpAwarded);
        Assert.InRange(session.EndedAt!.Value, before, after);
    }

    [Fact]
    public async Task Should_FailAndKeepSingleXpGain_When_CompletionRequestedTwice()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, PastStart);
        await _sut.Handle(new CompleteSessionCommand());

        var second = await _sut.Handle(new CompleteSessionCommand());

        Assert.False(second.IsSuccess);
        Assert.Equal(PastStart.AddMinutes(25), session.EndedAt);
        Assert.Single(_xpGainRepository.Gains);
    }

    [Fact]
    public async Task Should_SetSprintNumber_When_SprintCompletedLateOnAnotherUtcDay()
    {
        // The start entry sits on day J; the completion is reported later but dated at the
        // scheduled end (day J), so the sprint number is found.
        _journalRepository.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), PastStart, FakeCurrentUserService.TestUserId, 1));
        AddActiveSession(PomodoroSessionType.Sprint, PastStart);

        await _sut.Handle(new CompleteSessionCommand());

        var entry = _journalRepository.Entries.Single(e => e.EventType == JournalEventType.SprintCompleted);
        Assert.Equal(1, entry.Sequence);
    }

    [Fact]
    public async Task Should_NotSetSprintNumber_When_SprintSpansUtcMidnight()
    {
        // Known limit (V5): start on day J, scheduled end on J+1 — no SprintStarted entry on J+1.
        var start = new DateTime(2026, 3, 10, 23, 50, 0, DateTimeKind.Utc);
        _journalRepository.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), start, FakeCurrentUserService.TestUserId, 1));
        AddActiveSession(PomodoroSessionType.Sprint, start);

        await _sut.Handle(new CompleteSessionCommand());

        var entry = _journalRepository.Entries.Single(e => e.EventType == JournalEventType.SprintCompleted);
        Assert.Null(entry.Sequence);
        Assert.Equal(start.AddMinutes(25), entry.OccurredAt);
    }
}
