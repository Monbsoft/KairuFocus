using KairuFocus.Domain.Gamification;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityPresence;

public sealed class GetCommunityPresenceQueryHandler
    : IQueryHandler<GetCommunityPresenceQuery, GetCommunityPresenceResult>
{
    private readonly ICommunityActivityRepository _activityRepository;
    private readonly TimeProvider _timeProvider;

    public GetCommunityPresenceQueryHandler(
        ICommunityActivityRepository activityRepository,
        TimeProvider timeProvider)
    {
        _activityRepository = activityRepository;
        _timeProvider = timeProvider;
    }

    public async Task<GetCommunityPresenceResult> Handle(
        GetCommunityPresenceQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var inSprintNow = await _activityRepository.CountSprintsInProgressAsync(now, cancellationToken);

        // Fallback line: members who completed at least one sprint during the current UTC day.
        var todayUtc = now.Date;
        var sprintedToday = await _activityRepository.CountActiveMembersAsync(todayUtc, todayUtc.AddDays(1), cancellationToken);

        return new GetCommunityPresenceResult(inSprintNow, sprintedToday);
    }
}
