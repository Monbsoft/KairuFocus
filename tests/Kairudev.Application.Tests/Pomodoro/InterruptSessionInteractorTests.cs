using Kairudev.Application.Pomodoro.InterruptSession;
using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Tests.Pomodoro;

public sealed class InterruptSessionInteractorTests
{
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakePresenter _presenter = new();
    private readonly InterruptSessionInteractor _sut;

    public InterruptSessionInteractorTests() =>
        _sut = new InterruptSessionInteractor(_sessions, _presenter);

    private void AddActiveSession()
    {
        var session = PomodoroSession.Create(25);
        session.Start(DateTime.UtcNow.AddMinutes(-5));
        _sessions.Sessions.Add(session);
    }

    [Fact]
    public async Task Should_PresentSuccess_When_ActiveSessionExists()
    {
        AddActiveSession();

        await _sut.Execute(new InterruptSessionRequest());

        Assert.True(_presenter.IsSuccess);
    }

    [Fact]
    public async Task Should_PresentFailure_When_NoActiveSession()
    {
        await _sut.Execute(new InterruptSessionRequest());

        Assert.False(_presenter.IsSuccess);
        Assert.NotNull(_presenter.FailureReason);
    }

    private sealed class FakePresenter : IInterruptSessionPresenter
    {
        public bool IsSuccess { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess() => IsSuccess = true;
        public void PresentFailure(string reason) => FailureReason = reason;
    }
}
