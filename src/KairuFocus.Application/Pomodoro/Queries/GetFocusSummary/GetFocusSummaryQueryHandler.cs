using KairuFocus.Application.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

public sealed class GetFocusSummaryQueryHandler
    : IQueryHandler<GetFocusSummaryQuery, GetFocusSummaryResult>
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetFocusSummaryQueryHandler> _logger;

    public GetFocusSummaryQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        ICurrentUserService currentUserService,
        ILogger<GetFocusSummaryQueryHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetFocusSummaryResult> Handle(
        GetFocusSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Building focus summary for user {UserId}", userId);

        var sprintsToday = await _sessionRepository.GetCompletedSprintsTodayCountAsync(userId, cancellationToken);

        var sessions = await _sessionRepository.GetCompletedSprintSessionsTodayAsync(userId, cancellationToken);
        var totalSeconds = sessions.Sum(s =>
            s.StartedAt.HasValue && s.EndedAt.HasValue
                ? (s.EndedAt.Value - s.StartedAt.Value).TotalSeconds
                : 0d);
        var focusMinutesToday = (int)Math.Round(totalSeconds / 60.0);

        var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        var dailyGoal = settings.DailySprintGoal;

        var completedDates = await _sessionRepository.GetCompletedSprintDatesAsync(userId, cancellationToken);
        var streak = StreakCalculator.Compute(completedDates, DateOnly.FromDateTime(DateTime.UtcNow));

        _logger.LogDebug(
            "Focus summary for user {UserId}: sprints={Sprints}, minutes={Minutes}, goal={Goal}, streak={Streak}",
            userId, sprintsToday, focusMinutesToday, dailyGoal, streak);

        return new GetFocusSummaryResult(sprintsToday, focusMinutesToday, dailyGoal, streak);
    }
}
