using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.ConsentToPresence;

public sealed class ConsentToPresenceCommandHandler : ICommandHandler<ConsentToPresenceCommand, ConsentToPresenceResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IMemberPresenceRepository _presenceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public ConsentToPresenceCommandHandler(
        IMemberProfileRepository profileRepository,
        IMemberPresenceRepository presenceRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _profileRepository = profileRepository;
        _presenceRepository = presenceRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<ConsentToPresenceResult> Handle(
        ConsentToPresenceCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return ConsentToPresenceResult.Failure(CommunityErrors.Community.NotMember);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        profile.ConsentToPresence(now);

        if (!await _profileRepository.TryUpdateAsync(profile, cancellationToken))
            return ConsentToPresenceResult.Failure(CommunityErrors.Community.NotMember);

        // The member is shown connected right away, without waiting for the next heartbeat.
        await _presenceRepository.RecordSeenAsync(userId, now, cancellationToken);
        return ConsentToPresenceResult.Success();
    }
}
