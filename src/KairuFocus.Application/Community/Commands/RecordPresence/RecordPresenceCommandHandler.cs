using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.RecordPresence;

public sealed class RecordPresenceCommandHandler : ICommandHandler<RecordPresenceCommand, RecordPresenceResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IMemberPresenceRepository _presenceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public RecordPresenceCommandHandler(
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

    public async Task<RecordPresenceResult> Handle(
        RecordPresenceCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null || !profile.HasConsentedToPresence)
            return new RecordPresenceResult(false);

        await _presenceRepository.RecordSeenAsync(userId, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return new RecordPresenceResult(true);
    }
}
