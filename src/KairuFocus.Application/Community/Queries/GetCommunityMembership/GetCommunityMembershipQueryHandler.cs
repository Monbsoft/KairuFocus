using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetCommunityMembership;

public sealed class GetCommunityMembershipQueryHandler : IQueryHandler<GetCommunityMembershipQuery, MembershipViewModel>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCommunityMembershipQueryHandler(
        IMemberProfileRepository profileRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MembershipViewModel> Handle(
        GetCommunityMembershipQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        // Without a user row there is no login to propose.
        var loginAvailable = user is not null
            && await new ProfileNameResolver(_userRepository, _profileRepository)
                .IsGitHubLoginAvailableAsync(user, profile?.NameKey, cancellationToken);

        return profile is null
            ? MembershipViewModel.NotMember(user?.Login, loginAvailable)
            : MembershipViewModel.ForMember(profile, user?.Login, loginAvailable);
    }
}
