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
    private readonly TimeProvider _timeProvider;

    public GetFocusSummaryQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        ICurrentUserService currentUserService,
        ILogger<GetFocusSummaryQueryHandler> logger,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<GetFocusSummaryResult> Handle(
        GetFocusSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Building focus summary for user {UserId} (offsetMinutes={Offset})", userId, query.OffsetMinutes);

        // Compute local "today" window expressed as a UTC range.
        var offset = TimeSpan.FromMinutes(query.OffsetMinutes);
        var nowLocal = _timeProvider.GetUtcNow().UtcDateTime + offset;  // ticks shifted to represent local clock
        var todayLocal = DateOnly.FromDateTime(nowLocal);
        var startUtc = nowLocal.Date - offset;            // UTC instant corresponding to local midnight today
        var endUtc = startUtc + TimeSpan.FromDays(1);

        var sprintsToday = await _sessionRepository.GetCompletedSprintsTodayCountAsync(userId, startUtc, endUtc, cancellationToken);

        var sessions = await _sessionRepository.GetCompletedSprintSessionsTodayAsync(userId, startUtc, endUtc, cancellationToken);
        var totalSeconds = sessions.Sum(s =>
            s.StartedAt.HasValue && s.EndedAt.HasValue
                ? (s.EndedAt.Value - s.StartedAt.Value).TotalSeconds
                : 0d);
        var focusMinutesToday = (int)Math.Round(totalSeconds / 60.0);

        var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        var dailyGoal = settings.DailySprintGoal;

        // Map each raw UTC end-time to a local date, then compute streak.
        // Bucketing is done here in the Application layer (ADR-020).
        var endTimes = await _sessionRepository.GetCompletedSprintEndTimesAsync(userId, cancellationToken);
        var localDates = endTimes
            .Select(t => DateOnly.FromDateTime(t + offset))
            .Distinct()
            .ToList();
        var streak = StreakCalculator.Compute(localDates, todayLocal);

        _logger.LogDebug(
            "Focus summary for user {UserId}: sprints={Sprints}, minutes={Minutes}, goal={Goal}, streak={Streak}",
            userId, sprintsToday, focusMinutesToday, dailyGoal, streak);

        return new GetFocusSummaryResult(sprintsToday, focusMinutesToday, dailyGoal, streak);
    }
}
