using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;
using KairuFocus.Domain.Community;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.LeaveCommunity;

/// <summary>Deletes the member profile and frees the name. No access to the XP ledger: the XP is kept as-is.</summary>
public sealed class LeaveCommunityCommandHandler : ICommandHandler<LeaveCommunityCommand, LeaveCommunityResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LeaveCommunityCommandHandler> _logger;

    public LeaveCommunityCommandHandler(
        IMemberProfileRepository profileRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<LeaveCommunityCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _mediator = mediator;
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

        // The group goes first: the departure needs the profile, and the removal is the point of no return.
        // A host stops it (HostLeftCommunity), a participant leaves it; being in no group is the usual case.
        var departure = await _mediator.DispatchAsync<LeaveOrStopGroupSessionCommand, LeaveOrStopGroupSessionResult>(
            new LeaveOrStopGroupSessionCommand(BecauseLeavingCommunity: true), cancellationToken);
        if (!departure.IsSuccess
            && departure.Error is not (CommunityErrors.Community.NotInGroup or CommunityErrors.Community.NotMember))
            return LeaveCommunityResult.Failure(departure.Error!);

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
