using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetCommunityMembership;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.JoinCommunity;

public sealed class JoinCommunityCommandHandler : ICommandHandler<JoinCommunityCommand, JoinCommunityResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JoinCommunityCommandHandler> _logger;

    public JoinCommunityCommandHandler(
        IMemberProfileRepository profileRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<JoinCommunityCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<JoinCommunityResult> Handle(
        JoinCommunityCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        if (await _profileRepository.GetByUserIdAsync(userId, cancellationToken) is not null)
            return JoinCommunityResult.Failure(CommunityErrors.Community.AlreadyMember);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return JoinCommunityResult.Failure(CommunityErrors.Community.UserNotFound);

        var resolver = new ProfileNameResolver(_userRepository, _profileRepository);
        var resolution = await resolver.ResolveAsync(
            user, command.Source, command.ChosenName, ownNameKey: null, cancellationToken);
        if (!resolution.IsSuccess)
            return JoinCommunityResult.Failure(resolution.Error!);

        var profile = MemberProfile.Join(
            userId,
            resolution.Name!,
            command.Source,
            resolution.AvatarUrl,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (!await _profileRepository.TryAddAsync(profile, cancellationToken))
        {
            // A uniqueness constraint rejected the insert: either a concurrent request of the same user
            // created the profile (UserId key), or someone else took the name in between.
            if (await _profileRepository.GetByUserIdAsync(userId, cancellationToken) is not null)
            {
                _logger.LogInformation("Concurrent community join for user {UserId}", userId);
                return JoinCommunityResult.Failure(CommunityErrors.Community.AlreadyMember);
            }

            _logger.LogInformation("Profile name was taken concurrently while user {UserId} was joining", userId);
            return JoinCommunityResult.Failure(CommunityErrors.Community.NameTaken);
        }

        _logger.LogInformation(
            "User {UserId} joined the community with a name of source {NameSource}", userId, command.Source);

        var loginAvailable = await resolver.IsGitHubLoginAvailableAsync(user, profile.NameKey, cancellationToken);

        return JoinCommunityResult.Success(MembershipViewModel.ForMember(profile, user.Login, loginAvailable));
    }
}
