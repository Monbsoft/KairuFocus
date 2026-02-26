using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.GetSuggestedSessionType;

public sealed class GetSuggestedSessionTypeInteractor : IGetSuggestedSessionTypeUseCase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly IGetSuggestedSessionTypePresenter _presenter;

    public GetSuggestedSessionTypeInteractor(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        IGetSuggestedSessionTypePresenter presenter)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _presenter = presenter;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        var completedCount = await _sessionRepository.GetCompletedTodayCountAsync(cancellationToken);

        var suggestedType = completedCount > 0 && completedCount % PomodoroSettings.SprintsBeforeLongBreak == 0
            ? PomodoroSessionType.LongBreak.ToString()
            : completedCount > 0
                ? PomodoroSessionType.ShortBreak.ToString()
                : PomodoroSessionType.Sprint.ToString();

        _presenter.PresentSuggestion(
            suggestedType,
            settings.SprintDurationMinutes,
            settings.ShortBreakDurationMinutes,
            settings.LongBreakDurationMinutes);
    }
}
