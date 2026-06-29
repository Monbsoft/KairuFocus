using KairuFocus.Application.Common;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusStats;

public sealed class GetFocusStatsQueryHandler
    : IQueryHandler<GetFocusStatsQuery, GetFocusStatsResult>
{
    /// <summary>
    /// Fenêtre maximale chargée : ~12 mois. Couvre la heatmap annuelle et toute période.
    /// Borne le set pour ne pas matérialiser tout l'historique de l'utilisateur.
    /// </summary>
    private const int LookbackDays = 365;

    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetFocusStatsQueryHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public GetFocusStatsQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        ILogger<GetFocusStatsQueryHandler> logger,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<GetFocusStatsResult> Handle(
        GetFocusStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        // Fenêtre jour local exprimée en UTC (offset borné dans LocalDayWindow.From).
        var window = LocalDayWindow.From(_timeProvider.GetUtcNow().UtcDateTime, query.OffsetMinutes);
        var sinceUtc = window.StartUtc.AddDays(-LookbackDays);

        var intervals = await _sessionRepository.GetCompletedSprintIntervalsAsync(userId, sinceUtc, cancellationToken);

        // Bucketing par date locale en couche Application (ADR-020/021).
        var days = FocusStatsAggregator.Aggregate(intervals, window.Offset);

        _logger.LogDebug(
            "Focus stats for user {UserId}: {DayCount} active days (offsetMinutes={Offset})",
            userId, days.Count, query.OffsetMinutes);

        return new GetFocusStatsResult(days);
    }
}
