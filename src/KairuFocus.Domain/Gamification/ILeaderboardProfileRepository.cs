using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Gamification;

/// <summary>Leaderboard profiles, accessed per user (filtered by UserId).</summary>
public interface ILeaderboardProfileRepository
{
    Task<LeaderboardProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<bool> IsPseudonymTakenAsync(Pseudonym pseudonym, CancellationToken cancellationToken = default);

    /// <summary>false if a uniqueness constraint rejected the insert (UserId key or pseudonym).</summary>
    Task<bool> TryAddAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default);

    /// <summary>false if the unique pseudonym index rejected the update.</summary>
    Task<bool> TryUpdateAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default);

    Task RemoveAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default);
}
