using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed class GetSuggestedSessionTypeQueryHandler
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;

    public GetSuggestedSessionTypeQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<GetSuggestedSessionTypeResult> HandleAsync(
        GetSuggestedSessionTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        var allCompletedToday = await _sessionRepository.GetCompletedTodayCountAsync(cancellationToken);
        var sprintsToday = await _sessionRepository.GetCompletedSprintsTodayCountAsync(cancellationToken);
        var breaksToday = allCompletedToday - sprintsToday;

        PomodoroSessionType suggestedType;
        if (sprintsToday > breaksToday)
        {
            // Dernier cycle était un sprint → suggérer une pause
            suggestedType = sprintsToday % PomodoroSettings.SprintsBeforeLongBreak == 0
                ? PomodoroSessionType.LongBreak
                : PomodoroSessionType.ShortBreak;
        }
        else
        {
            // Dernier cycle était une pause (ou aucune session) → suggérer un sprint
            suggestedType = PomodoroSessionType.Sprint;
        }

        return new GetSuggestedSessionTypeResult(
            suggestedType.ToString(),
            settings.SprintDurationMinutes,
            settings.ShortBreakDurationMinutes,
            settings.LongBreakDurationMinutes);
    }
}
