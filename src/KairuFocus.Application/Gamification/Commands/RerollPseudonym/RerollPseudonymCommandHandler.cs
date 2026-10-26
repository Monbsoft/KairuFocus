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
    private readonly Random _random;

    public RerollPseudonymCommandHandler(
        ILeaderboardProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        ILogger<RerollPseudonymCommandHandler> logger)
        : this(profileRepository, currentUserService, logger, Random.Shared)
    {
    }

    /// <summary>
    /// Test seam: a seeded <see cref="Random"/> makes the draws deterministic. Dependency injection only
    /// sees the public constructor.
    /// </summary>
    internal RerollPseudonymCommandHandler(
        ILeaderboardProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        ILogger<RerollPseudonymCommandHandler> logger,
        Random random)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _random = random;
    }

    public async Task<RerollPseudonymResult> Handle(
        RerollPseudonymCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return RerollPseudonymResult.Failure(GamificationErrors.Gamification.NotParticipating);

        // The current pseudonym is excluded explicitly: the store cannot be trusted to report it taken, since
        // a concurrent reroll or leave (another tab) may have freed it since the read. A concurrent leave is
        // then detected by the rejected update below.
        var pseudonym = await PseudonymAllocator.AllocateAsync(
            _profileRepository, _random, excluded: profile.Pseudonym, cancellationToken);
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
            // The update was rejected: either a concurrent request (another tab) removed the profile, or
            // someone else took the drawn pseudonym in between.
            if (await _profileRepository.GetByUserIdAsync(userId, cancellationToken) is null)
            {
                _logger.LogInformation("Concurrent leaderboard leave while user {UserId} was rerolling", userId);
                return RerollPseudonymResult.Failure(GamificationErrors.Gamification.NotParticipating);
            }

            _logger.LogWarning("Drawn pseudonym was taken concurrently while user {UserId} was rerolling", userId);
            return RerollPseudonymResult.Failure(GamificationErrors.Gamification.PseudonymPoolExhausted);
        }

        _logger.LogInformation("User {UserId} drew a new leaderboard pseudonym", userId);

        return RerollPseudonymResult.Success(pseudonym.Value);
    }
}
