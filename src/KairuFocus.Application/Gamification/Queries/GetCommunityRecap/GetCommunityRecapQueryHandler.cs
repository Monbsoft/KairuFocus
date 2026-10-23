using KairuFocus.Application.Common;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityRecap;

public sealed class GetCommunityRecapQueryHandler
    : IQueryHandler<GetCommunityRecapQuery, GetCommunityRecapResult>
{
    private static readonly GetCommunityRecapResult Empty = new(Sprints: 0, FocusMinutes: 0, ActiveMembers: 0, MySprints: 0);

    private readonly ICommunityActivityRepository _activityRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GetCommunityRecapQueryHandler> _logger;

    public GetCommunityRecapQueryHandler(
        ICommunityActivityRepository activityRepository,
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<GetCommunityRecapQueryHandler> logger)
    {
        _activityRepository = activityRepository;
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<GetCommunityRecapResult> Handle(
        GetCommunityRecapQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Global starts when XP went live: the oldest gain of the whole ledger.
        DateTime? firstXpGainEver = query.Period == CommunityPeriod.Global
            ? await _activityRepository.GetFirstXpGainUtcAsync(cancellationToken)
            : null;

        if (CommunityCalendar.CurrentWindow(query.Period, now, firstXpGainEver) is not { } window)
            return Empty;

        var totals = await _activityRepository.GetCompletedSprintTotalsAsync(window.StartUtc, window.EndUtc, cancellationToken);
        var activeMembers = await _activityRepository.CountActiveMembersAsync(window.StartUtc, window.EndUtc, cancellationToken);

        // Despite its name, this existing query counts completed sprints over any [start, end) window.
        var mySprints = await _sessionRepository.GetCompletedSprintsTodayCountAsync(userId, window.StartUtc, window.EndUtc, cancellationToken);

        _logger.LogDebug(
            "Community recap ({Period}): sprints={Sprints}, activeMembers={ActiveMembers}",
            query.Period, totals.Sprints, activeMembers);

        return new GetCommunityRecapResult(
            totals.Sprints,
            SprintActivityCalculator.ToRoundedMinutes(totals.Focus),
            activeMembers,
            mySprints);
    }
}
