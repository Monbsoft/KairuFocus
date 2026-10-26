using KairuFocus.Application.Common;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

public sealed class GetCommunityProgressQueryHandler
    : IQueryHandler<GetCommunityProgressQuery, GetCommunityProgressResult>
{
    private readonly IXpGainRepository _xpGainRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GetCommunityProgressQueryHandler> _logger;

    public GetCommunityProgressQueryHandler(
        IXpGainRepository xpGainRepository,
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<GetCommunityProgressQueryHandler> logger)
    {
        _xpGainRepository = xpGainRepository;
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<GetCommunityProgressResult> Handle(
        GetCommunityProgressQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var earnings = await _xpGainRepository.GetEarningsAsync(userId, cancellationToken);
        var progress = XpProgressCalculator.ComputeXp(earnings, query.Period, now);

        DateTime? firstGain = earnings.Count > 0 ? earnings.Min(e => e.EarnedAtUtc) : null;

        // No window only for "global" without any gain: nothing to count.
        if (CommunityCalendar.CurrentWindow(query.Period, now, firstGain) is not { } window)
            return progress;

        // One read covers both the window and the all-time count since the first gain.
        var sinceUtc = firstGain is { } first && first < window.StartUtc ? first : window.StartUtc;
        var intervals = await _sessionRepository.GetCompletedSprintIntervalsAsync(userId, sinceUtc, cancellationToken);

        var (sprints, focus) = SprintActivityCalculator.Summarize(intervals, window);
        var allTimeSprints = firstGain is { } firstGainUtc ? intervals.Count(i => i.EndedAt >= firstGainUtc) : 0;

        _logger.LogDebug(
            "Community progress for user {UserId} ({Period}): xp={Xp}, sprints={Sprints}, allTimeSprints={AllTimeSprints}",
            userId, query.Period, progress.Xp, sprints, allTimeSprints);

        return progress with
        {
            Sprints = sprints,
            FocusMinutes = SprintActivityCalculator.ToRoundedMinutes(focus),
            AllTime = progress.AllTime with { Sprints = allTimeSprints },
        };
    }
}
