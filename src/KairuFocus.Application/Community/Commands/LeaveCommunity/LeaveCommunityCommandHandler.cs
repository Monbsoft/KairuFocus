using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.LeaveCommunity;

/// <summary>Deletes the member profile and frees the name. No access to the XP ledger: the XP is kept as-is.</summary>
public sealed class LeaveCommunityCommandHandler : ICommandHandler<LeaveCommunityCommand, LeaveCommunityResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LeaveCommunityCommandHandler> _logger;

    public LeaveCommunityCommandHandler(
        IMemberProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        ILogger<LeaveCommunityCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<LeaveCommunityResult> Handle(
        LeaveCommunityCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return LeaveCommunityResult.Failure(CommunityErrors.Community.NotMember);

        if (!await _profileRepository.TryRemoveAsync(profile, cancellationToken))
        {
            // A concurrent request (another tab) removed the profile between the read and the delete.
            _logger.LogInformation("Concurrent community leave for user {UserId}", userId);
            return LeaveCommunityResult.Failure(CommunityErrors.Community.NotMember);
        }

        _logger.LogInformation("User {UserId} left the community", userId);

        return LeaveCommunityResult.Success();
    }
}
