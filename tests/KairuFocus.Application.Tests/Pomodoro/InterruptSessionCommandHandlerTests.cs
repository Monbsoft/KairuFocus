using KairuFocus.Application.Pomodoro.Commands.InterruptSession;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class InterruptSessionCommandHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakeJournalEntryRepository _journalRepository = new();
    private readonly FakeMediator _fakeMediator;
    private readonly InterruptSessionCommandHandler _sut;

    public InterruptSessionCommandHandlerTests()
    {
        _fakeMediator = new FakeMediator(_journalRepository);
        _sut = new InterruptSessionCommandHandler(
            _sessionRepository,
            _fakeMediator,
            new FakeCurrentUserService(),
            NullLogger<InterruptSessionCommandHandler>.Instance);
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
        var result = await _sut.Handle(new InterruptSessionCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.NoActiveSession, result.Error);
    }

    [Fact]
    public async Task Should_LogSprintInterrupted_When_SprintSessionInterrupted()
    {
        AddActiveSession(PomodoroSessionType.Sprint);

        var result = await _sut.Handle(new InterruptSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.SprintInterrupted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_LogBreakInterrupted_When_ShortBreakSessionInterrupted()
    {
        AddActiveSession(PomodoroSessionType.ShortBreak);

        var result = await _sut.Handle(new InterruptSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.BreakInterrupted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_LogBreakInterrupted_When_LongBreakSessionInterrupted()
    {
        AddActiveSession(PomodoroSessionType.LongBreak);

        var result = await _sut.Handle(new InterruptSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.BreakInterrupted, _journalRepository.Entries[0].EventType);
    }

    // ── Bounded end time (ADR-025) ─────────────────────────────────────────

    private static readonly DateTime PastStart = new(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_SetEndedAtToScheduledEndAndStayInterrupted_When_InterruptedLate()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, PastStart);

        var result = await _sut.Handle(new InterruptSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(PastStart.AddMinutes(25), session.EndedAt);
    }

    [Fact]
    public async Task Should_DateJournalEntryAtEndedAt_When_SessionInterrupted()
    {
        AddActiveSession(PomodoroSessionType.Sprint, PastStart);

        await _sut.Handle(new InterruptSessionCommand());

        var entry = Assert.Single(_journalRepository.Entries);
        Assert.Equal(PastStart.AddMinutes(25), entry.OccurredAt);
    }

    [Fact]
    public async Task Should_KeepRequestTimeAsEndedAt_When_FreeSprintInterrupted()
    {
        var session = AddActiveSession(PomodoroSessionType.Sprint, DateTime.UtcNow.AddHours(-3), plannedDurationMinutes: 0);

        var before = DateTime.UtcNow;
        await _sut.Handle(new InterruptSessionCommand());
        var after = DateTime.UtcNow;

        Assert.InRange(session.EndedAt!.Value, before, after);
    }

    [Fact]
    public async Task Should_InterruptSession_When_JournalEntryThrows()
    {
        // The interruption is already persisted: a failing journal must NEVER fail it (ADR-023).
        var session = AddActiveSession(PomodoroSessionType.Sprint);
        _fakeMediator.ThrowOnCreateEntry = true;

        var result = await _sut.Handle(new InterruptSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(1, _sessionRepository.UpdateAsyncCallCount);
        Assert.Empty(_journalRepository.Entries);
    }
}
