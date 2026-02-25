using Kairudev.Application.Pomodoro.Common;
using Kairudev.Application.Pomodoro.StartSession;

namespace Kairudev.Application.Tests.Pomodoro;

public sealed class StartSessionInteractorTests
{
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakePomodoroSettingsRepository _settings = new();
    private readonly FakePresenter _presenter = new();
    private readonly StartSessionInteractor _sut;

    public StartSessionInteractorTests() =>
        _sut = new StartSessionInteractor(_sessions, _settings, _presenter);

    [Fact]
    public async Task Should_PresentSuccess_When_NoActiveSession()
    {
        await _sut.Execute(new StartSessionRequest());

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.Session);
        Assert.Equal("Active", _presenter.Session.Status);
    }

    [Fact]
    public async Task Should_UseSettingsSprintDuration_When_Starting()
    {
        _settings.Settings = Domain.Pomodoro.PomodoroSettings.Create(45, 10, 20).Value;

        await _sut.Execute(new StartSessionRequest());

        Assert.Equal(45, _presenter.Session!.PlannedDurationMinutes);
    }

    [Fact]
    public async Task Should_PersistSession_When_Started()
    {
        await _sut.Execute(new StartSessionRequest());

        Assert.Single(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_PresentFailure_When_SessionAlreadyActive()
    {
        await _sut.Execute(new StartSessionRequest());
        var secondPresenter = new FakePresenter();

        await new StartSessionInteractor(_sessions, _settings, secondPresenter)
            .Execute(new StartSessionRequest());

        Assert.False(secondPresenter.IsSuccess);
        Assert.NotNull(secondPresenter.FailureReason);
    }

    private sealed class FakePresenter : IStartSessionPresenter
    {
        public PomodoroSessionViewModel? Session { get; private set; }
        public string? FailureReason { get; private set; }
        public bool IsSuccess { get; private set; }

        public void PresentSuccess(PomodoroSessionViewModel session) { Session = session; IsSuccess = true; }
        public void PresentFailure(string reason) => FailureReason = reason;
    }
}
