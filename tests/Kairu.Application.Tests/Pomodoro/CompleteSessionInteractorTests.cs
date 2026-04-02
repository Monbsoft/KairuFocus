using Kairu.Application.Journal.CreateJournalEntry;
using Kairu.Application.Pomodoro.CompleteSession;
using Kairu.Domain.Pomodoro;

namespace Kairu.Application.Tests.Pomodoro;

public sealed class CompleteSessionInteractorTests
{
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakePomodoroSettingsRepository _settings = new();
    private readonly FakePresenter _presenter = new();
    private readonly NoOpCreateJournalEntryUseCase _noOpJournal = new();
    private readonly CompleteSessionInteractor _sut;

    public CompleteSessionInteractorTests() =>
        _sut = new CompleteSessionInteractor(_sessions, _settings, _presenter, _noOpJournal);

    private PomodoroSession AddActiveSession()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25);
        session.Start(DateTime.UtcNow.AddMinutes(-25));
        _sessions.Sessions.Add(session);
        return session;
    }

    [Fact]
    public async Task Should_PresentSuccess_When_ActiveSessionExists()
    {
        AddActiveSession();

        await _sut.Execute(new CompleteSessionRequest());

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.Result);
    }

    [Fact]
    public async Task Should_ReturnShortBreak_When_LessThanFourSprintsCompleted()
    {
        AddActiveSession();

        await _sut.Execute(new CompleteSessionRequest());

        Assert.Equal("short", _presenter.Result!.BreakType);
        Assert.Equal(_settings.Settings.ShortBreakDurationMinutes, _presenter.Result.BreakDurationMinutes);
    }

    [Fact]
    public async Task Should_ReturnLongBreak_When_FourthSprintCompleted()
    {
        // Simulate 3 already completed sessions
        for (var i = 0; i < 3; i++)
        {
            var s = PomodoroSession.Create(PomodoroSessionType.Sprint, 25);
            s.Start(DateTime.UtcNow.AddHours(-i - 1));
            s.Complete(DateTime.UtcNow.AddMinutes(-i * 60));
            _sessions.Sessions.Add(s);
        }
        AddActiveSession();

        await _sut.Execute(new CompleteSessionRequest());

        Assert.Equal("long", _presenter.Result!.BreakType);
        Assert.Equal(_settings.Settings.LongBreakDurationMinutes, _presenter.Result.BreakDurationMinutes);
    }

    [Fact]
    public async Task Should_PresentFailure_When_NoActiveSession()
    {
        await _sut.Execute(new CompleteSessionRequest());

        Assert.False(_presenter.IsSuccess);
        Assert.NotNull(_presenter.FailureReason);
    }

    private sealed class NoOpCreateJournalEntryUseCase : ICreateJournalEntryUseCase
    {
        public Task Execute(CreateJournalEntryRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakePresenter : ICompleteSessionPresenter
    {
        public CompleteSessionResult? Result { get; private set; }
        public string? FailureReason { get; private set; }
        public bool IsSuccess { get; private set; }

        public void PresentSuccess(CompleteSessionResult result) { Result = result; IsSuccess = true; }
        public void PresentFailure(string reason) => FailureReason = reason;
    }
}
