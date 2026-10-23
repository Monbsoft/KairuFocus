using KairuFocus.Application.Common;
using KairuFocus.Domain.Gamification;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Application.Gamification.Commands.LeaveLeaderboard;

/// <summary>Deletes the leaderboard profile. No access to the XP ledger: the user's XP is kept as-is.</summary>
public sealed class LeaveLeaderboardCommandHandler : ICommandHandler<LeaveLeaderboardCommand, LeaveLeaderboardResult>
{
    private readonly ILeaderboardProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LeaveLeaderboardCommandHandler> _logger;

    public LeaveLeaderboardCommandHandler(
        ILeaderboardProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        ILogger<LeaveLeaderboardCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<LeaveLeaderboardResult> Handle(
        LeaveLeaderboardCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return LeaveLeaderboardResult.Failure(GamificationErrors.Gamification.NotParticipating);

        if (!await _profileRepository.TryRemoveAsync(profile, cancellationToken))
        {
            // A concurrent request (another tab) removed the profile between the read and the delete.
            _logger.LogInformation("Concurrent leaderboard leave for user {UserId}", userId);
            return LeaveLeaderboardResult.Failure(GamificationErrors.Gamification.NotParticipating);
        }

        _logger.LogInformation("User {UserId} left the community leaderboard", userId);

        return LeaveLeaderboardResult.Success();
    }
}
