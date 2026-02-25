using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.CompleteSession;

public sealed class CompleteSessionInteractor : ICompleteSessionUseCase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly ICompleteSessionPresenter _presenter;

    public CompleteSessionInteractor(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        ICompleteSessionPresenter presenter)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _presenter = presenter;
    }

    public async Task Execute(CompleteSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            _presenter.PresentFailure(DomainErrors.Pomodoro.SessionNotActive);
            return;
        }

        session.Complete(DateTime.UtcNow);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        var completedCount = await _sessionRepository.GetCompletedTodayCountAsync(cancellationToken);
        var settings = await _settingsRepository.GetAsync(cancellationToken);

        var isLongBreak = completedCount % PomodoroSettings.SprintsBeforeLongBreak == 0;
        var breakType = isLongBreak ? "long" : "short";
        var breakDuration = isLongBreak
            ? settings.LongBreakDurationMinutes
            : settings.ShortBreakDurationMinutes;

        _presenter.PresentSuccess(new CompleteSessionResult(breakType, breakDuration));
    }
}
