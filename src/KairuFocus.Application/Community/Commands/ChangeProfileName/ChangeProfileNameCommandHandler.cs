using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetCommunityMembership;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.ChangeProfileName;

public sealed class ChangeProfileNameCommandHandler : ICommandHandler<ChangeProfileNameCommand, ChangeProfileNameResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChangeProfileNameCommandHandler> _logger;

    public ChangeProfileNameCommandHandler(
        IMemberProfileRepository profileRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<ChangeProfileNameCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ChangeProfileNameResult> Handle(
        ChangeProfileNameCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return ChangeProfileNameResult.Failure(CommunityErrors.Community.NotMember);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return ChangeProfileNameResult.Failure(CommunityErrors.Community.UserNotFound);

        var resolver = new ProfileNameResolver(_userRepository, _profileRepository);
        var resolution = await resolver.ResolveAsync(
            user, command.Source, command.ChosenName, profile.NameKey, cancellationToken);
        if (!resolution.IsSuccess)
            return ChangeProfileNameResult.Failure(resolution.Error!);

        var name = resolution.Name!;
        // Saving the GitHub name again is the explicit gesture that refreshes the avatar (no automatic sync).
        var unchanged = name.Value == profile.Name.Value
            && command.Source == profile.NameSource
            && resolution.AvatarUrl == profile.AvatarUrl;

        if (!unchanged)
        {
            profile.ChangeName(name, command.Source, resolution.AvatarUrl);

            if (!await _profileRepository.TryUpdateAsync(profile, cancellationToken))
            {
                // The update affected no row (profile deleted concurrently) or the unique index on the
                // name key rejected it (name taken concurrently).
                if (await _profileRepository.GetByUserIdAsync(userId, cancellationToken) is null)
                {
                    _logger.LogInformation("Community profile of user {UserId} was deleted during a rename", userId);
                    return ChangeProfileNameResult.Failure(CommunityErrors.Community.NotMember);
                }

                _logger.LogInformation("Profile name was taken concurrently while user {UserId} was renaming", userId);
                return ChangeProfileNameResult.Failure(CommunityErrors.Community.NameTaken);
            }

            _logger.LogInformation(
                "User {UserId} changed their profile name to a name of source {NameSource}", userId, command.Source);
        }

        var loginAvailable = await resolver.IsGitHubLoginAvailableAsync(user, profile.NameKey, cancellationToken);

        return ChangeProfileNameResult.Success(MembershipViewModel.ForMember(profile, user.Login, loginAvailable));
    }
}
