using KairuFocus.Application.Common;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

public sealed class GetCommunityLeaderboardQueryHandler
    : IQueryHandler<GetCommunityLeaderboardQuery, GetCommunityLeaderboardResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly ILeaderboardStandingsRepository _standingsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GetCommunityLeaderboardQueryHandler> _logger;

    public GetCommunityLeaderboardQueryHandler(
        IMemberProfileRepository profileRepository,
        ILeaderboardStandingsRepository standingsRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<GetCommunityLeaderboardQueryHandler> logger)
    {
        _profileRepository = profileRepository;
        _standingsRepository = standingsRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<GetCommunityLeaderboardResult> Handle(
        GetCommunityLeaderboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);

        IReadOnlyList<XpStanding> standings;
        if (query.Period == CommunityPeriod.Global)
        {
            standings = await _standingsRepository.GetAllTimeStandingsAsync(cancellationToken);
        }
        else
        {
            var window = CommunityCalendar.CurrentWindow(query.Period, now, firstGainUtc: null)!.Value;
            standings = await _standingsRepository.GetStandingsBetweenAsync(window.StartUtc, window.EndUtc, cancellationToken);
        }

        var result = LeaderboardRanker.Rank(standings, profile?.Name, isParticipant: profile is not null);

        // Other members' names are never logged.
        _logger.LogDebug(
            "Community leaderboard for user {UserId} ({Period}): {RankedCount} ranked members",
            userId, query.Period, result.RankedCount);

        return result;
    }
}
