using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Gamification;

/// <summary>Leaderboard profiles, accessed per user (filtered by UserId).</summary>
public interface ILeaderboardProfileRepository
{
    Task<LeaderboardProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<bool> IsPseudonymTakenAsync(Pseudonym pseudonym, CancellationToken cancellationToken = default);

    /// <summary>false if a uniqueness constraint rejected the insert (UserId key or pseudonym).</summary>
    Task<bool> TryAddAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// false if the update was rejected: the unique pseudonym index refused the new pseudonym, or the
    /// profile was deleted concurrently. The caller tells them apart by re-reading the profile.
    /// </summary>
    Task<bool> TryUpdateAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default);

    /// <summary>false if the profile was already deleted concurrently (nothing left to remove).</summary>
    Task<bool> TryRemoveAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default);
}
