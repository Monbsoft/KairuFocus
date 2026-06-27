using KairuFocus.Application.Common;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed class GetSuggestedSessionTypeQueryHandler : IQueryHandler<GetSuggestedSessionTypeQuery, GetSuggestedSessionTypeResult>
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetSuggestedSessionTypeQueryHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public GetSuggestedSessionTypeQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        ICurrentUserService currentUserService,
        ILogger<GetSuggestedSessionTypeQueryHandler> logger,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _timeProvider = timeProvider;
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
            // Use the user's local-day window (aligned with GetFocusSummaryQueryHandler)
            // so that both "sprints today" counts are consistent near midnight.
            // Offset is clamped to a valid timezone range inside LocalDayWindow.From (correction E).
            var window = LocalDayWindow.From(_timeProvider.GetUtcNow().UtcDateTime, query.OffsetMinutes);
            var sprintsToday = await _sessionRepository.GetCompletedSprintsTodayCountAsync(
                userId, window.StartUtc, window.EndUtc, cancellationToken);

            suggestedType = sprintsToday % PomodoroSettings.SprintsBeforeLongBreak == 0
                ? PomodoroSessionType.LongBreak
                : PomodoroSessionType.ShortBreak;
        }
        else
        {
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
