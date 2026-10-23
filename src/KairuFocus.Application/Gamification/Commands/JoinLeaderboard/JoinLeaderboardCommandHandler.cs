using KairuFocus.Application.Common;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Application.Gamification.Commands.JoinLeaderboard;

public sealed class JoinLeaderboardCommandHandler : ICommandHandler<JoinLeaderboardCommand, JoinLeaderboardResult>
{
    private readonly ILeaderboardProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JoinLeaderboardCommandHandler> _logger;

    public JoinLeaderboardCommandHandler(
        ILeaderboardProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<JoinLeaderboardCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<JoinLeaderboardResult> Handle(
        JoinLeaderboardCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        if (await _profileRepository.GetByUserIdAsync(userId, cancellationToken) is not null)
            return JoinLeaderboardResult.Failure(GamificationErrors.Gamification.AlreadyParticipating);

        var pseudonym = await PseudonymAllocator.AllocateAsync(_profileRepository, Random.Shared, cancellationToken);
        if (pseudonym is null)
        {
            _logger.LogError(
                "No free pseudonym after {Attempts} draws for user {UserId}",
                PseudonymAllocator.PlainAttempts + PseudonymAllocator.SuffixedAttempts, userId);
            return JoinLeaderboardResult.Failure(GamificationErrors.Gamification.PseudonymPoolExhausted);
        }

        var profile = LeaderboardProfile.Join(userId, pseudonym, _timeProvider.GetUtcNow().UtcDateTime);

        if (!await _profileRepository.TryAddAsync(profile, cancellationToken))
        {
            // A uniqueness constraint rejected the insert: either a concurrent request of the same user
            // created the profile (UserId key), or someone else took the pseudonym in between.
            if (await _profileRepository.GetByUserIdAsync(userId, cancellationToken) is not null)
            {
                _logger.LogInformation("Concurrent leaderboard join for user {UserId}", userId);
                return JoinLeaderboardResult.Failure(GamificationErrors.Gamification.AlreadyParticipating);
            }

            _logger.LogWarning("Drawn pseudonym was taken concurrently while user {UserId} was joining", userId);
            return JoinLeaderboardResult.Failure(GamificationErrors.Gamification.PseudonymPoolExhausted);
        }

        _logger.LogInformation("User {UserId} joined the community leaderboard", userId);

        return JoinLeaderboardResult.Success(pseudonym.Value);
    }
}
