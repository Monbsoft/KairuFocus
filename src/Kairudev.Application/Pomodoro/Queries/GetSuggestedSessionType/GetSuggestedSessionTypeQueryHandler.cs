using Kairudev.Application.Common;
using Kairudev.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed class GetSuggestedSessionTypeQueryHandler : IQueryHandler<GetSuggestedSessionTypeQuery, GetSuggestedSessionTypeResult>
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetSuggestedSessionTypeQueryHandler> _logger;

    public GetSuggestedSessionTypeQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        ICurrentUserService currentUserService,
        ILogger<GetSuggestedSessionTypeQueryHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetSuggestedSessionTypeResult> Handle(
        GetSuggestedSessionTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Computing suggested session type for user {UserId}", userId);

        var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        var latestSession = await _sessionRepository.GetLatestCompletedTodayAsync(userId, cancellationToken);

        PomodoroSessionType suggestedType;
        if (latestSession?.SessionType == PomodoroSessionType.Sprint)
        {
            var sprintsToday = await _sessionRepository.GetCompletedSprintsTodayCountAsync(userId, cancellationToken);
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

        _logger.LogDebug("Suggested session type for user {UserId} is {SuggestedType}", userId, suggestedType);

        return new GetSuggestedSessionTypeResult(
            suggestedType,
            settings.SprintDurationMinutes,
            settings.ShortBreakDurationMinutes,
            settings.LongBreakDurationMinutes);
    }
}
