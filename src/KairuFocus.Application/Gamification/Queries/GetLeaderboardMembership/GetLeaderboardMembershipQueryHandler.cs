using KairuFocus.Application.Common;
using KairuFocus.Domain.Gamification;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetLeaderboardMembership;

public sealed class GetLeaderboardMembershipQueryHandler
    : IQueryHandler<GetLeaderboardMembershipQuery, GetLeaderboardMembershipResult>
{
    private readonly ILeaderboardProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetLeaderboardMembershipQueryHandler(
        ILeaderboardProfileRepository profileRepository,
        ICurrentUserService currentUserService)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GetLeaderboardMembershipResult> Handle(
        GetLeaderboardMembershipQuery query,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(_currentUserService.CurrentUserId, cancellationToken);

        return profile is null
            ? new GetLeaderboardMembershipResult(IsParticipant: false, Pseudonym: null)
            : new GetLeaderboardMembershipResult(IsParticipant: true, Pseudonym: profile.Pseudonym.Value);
    }
}
