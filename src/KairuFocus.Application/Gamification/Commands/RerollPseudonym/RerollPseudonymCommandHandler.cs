using KairuFocus.Application.Common;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Application.Gamification.Commands.RerollPseudonym;

public sealed class RerollPseudonymCommandHandler : ICommandHandler<RerollPseudonymCommand, RerollPseudonymResult>
{
    private readonly ILeaderboardProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RerollPseudonymCommandHandler> _logger;

    public RerollPseudonymCommandHandler(
        ILeaderboardProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        ILogger<RerollPseudonymCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RerollPseudonymResult> Handle(
        RerollPseudonymCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return RerollPseudonymResult.Failure(GamificationErrors.Gamification.NotParticipating);

        // The current pseudonym is held by this very profile, so the allocator never returns it.
        var pseudonym = await PseudonymAllocator.AllocateAsync(_profileRepository, Random.Shared, cancellationToken);
        if (pseudonym is null)
        {
            _logger.LogError(
                "No free pseudonym after {Attempts} draws for user {UserId}",
                PseudonymAllocator.PlainAttempts + PseudonymAllocator.SuffixedAttempts, userId);
            return RerollPseudonymResult.Failure(GamificationErrors.Gamification.PseudonymPoolExhausted);
        }

        profile.ChangePseudonym(pseudonym);

        if (!await _profileRepository.TryUpdateAsync(profile, cancellationToken))
        {
            _logger.LogWarning("Drawn pseudonym was taken concurrently while user {UserId} was rerolling", userId);
            return RerollPseudonymResult.Failure(GamificationErrors.Gamification.PseudonymPoolExhausted);
        }

        _logger.LogInformation("User {UserId} drew a new leaderboard pseudonym", userId);

        return RerollPseudonymResult.Success(pseudonym.Value);
    }
}
