using KairuFocus.Application.Common;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

public sealed class GetFocusSummaryQueryHandler
    : IQueryHandler<GetFocusSummaryQuery, GetFocusSummaryResult>
{
    /// <summary>
    /// Maximum streak window loaded from the repository.
    /// A streak cannot exceed this many consecutive days; beyond that the user
    /// has almost certainly missed a day. The bound prevents materialising the
    /// user's entire history on every dashboard load.
    /// </summary>
    private const int StreakLookbackDays = 366;

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
        // Offset is clamped to a valid timezone range inside LocalDayWindow.From (correction E).
        var window = LocalDayWindow.From(_timeProvider.GetUtcNow().UtcDateTime, query.OffsetMinutes);

        var sprintsToday = await _sessionRepository.GetCompletedSprintsTodayCountAsync(
            userId, window.StartUtc, window.EndUtc, cancellationToken);

        var sessions = await _sessionRepository.GetCompletedSprintSessionsTodayAsync(
            userId, window.StartUtc, window.EndUtc, cancellationToken);

        // Correction C: clamp StartedAt to the window start so that a sprint that began
        // before local midnight is not credited in full to today.
        // EndedAt is already guaranteed to be < EndUtc by the repository filter.
        var totalSeconds = sessions.Sum(s =>
        {
            if (!s.StartedAt.HasValue || !s.EndedAt.HasValue) return 0d;
            var effectiveStart = s.StartedAt.Value < window.StartUtc ? window.StartUtc : s.StartedAt.Value;
            return (s.EndedAt.Value - effectiveStart).TotalSeconds;
        });
        var focusMinutesToday = (int)Math.Round(totalSeconds / 60.0);

        var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        var dailyGoal = settings.DailySprintGoal;

        // Map each raw UTC end-time to a local date, then compute streak.
        // Bucketing is done here in the Application layer (ADR-020).
        // sinceUtc bounds the query to the last StreakLookbackDays days so that
        // the repository does not materialise the user's full history on every load.
        // Any streak longer than StreakLookbackDays is capped — an acceptable limit.
        var sinceUtc = window.StartUtc.AddDays(-StreakLookbackDays);
        var endTimes = await _sessionRepository.GetCompletedSprintEndTimesAsync(userId, sinceUtc, cancellationToken);
        var localDates = endTimes
            .Select(t => DateOnly.FromDateTime(t + window.Offset))
            .Distinct()
            .ToList();
        var streak = StreakCalculator.Compute(localDates, window.TodayLocal);

        _logger.LogDebug(
            "Focus summary for user {UserId}: sprints={Sprints}, minutes={Minutes}, goal={Goal}, streak={Streak}",
            userId, sprintsToday, focusMinutesToday, dailyGoal, streak);

        return new GetFocusSummaryResult(sprintsToday, focusMinutesToday, dailyGoal, streak);
    }
}
