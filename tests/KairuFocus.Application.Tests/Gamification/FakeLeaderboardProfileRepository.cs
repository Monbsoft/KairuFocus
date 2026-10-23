using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Gamification;

/// <summary>
/// In-memory leaderboard profiles. Stores snapshots, like a database: a profile returned by
/// <see cref="GetByUserIdAsync"/> is a fresh copy, and a rejected update leaves the stored row unchanged.
/// Simulates the primary key (UserId) and the unique pseudonym index.
/// </summary>
internal sealed class FakeLeaderboardProfileRepository : ILeaderboardProfileRepository
{
    private readonly List<StoredProfile> _stored = [];

    public IReadOnlyList<StoredProfile> Stored => _stored;

    /// <summary>
    /// Number of first candidates reported as taken, whatever their text: handler tests do not depend
    /// on the random draws.
    /// </summary>
    public int TakenCandidateCount { get; set; }

    /// <summary>Every candidate passed to <see cref="IsPseudonymTakenAsync"/>, in order.</summary>
    public List<Pseudonym> CheckedCandidates { get; } = [];

    /// <summary>
    /// Invoked at the beginning of TryAddAsync with the profile being inserted: lets a test insert the
    /// concurrent "winner" (same user, or another user with the same pseudonym).
    /// </summary>
    public Action<LeaderboardProfile>? OnTryAdd { get; set; }

    /// <summary>Invoked at the beginning of TryUpdateAsync: lets a test take the new pseudonym concurrently.</summary>
    public Action<LeaderboardProfile>? OnTryUpdate { get; set; }

    public int TryAddCallCount { get; private set; }
    public int TryUpdateCallCount { get; private set; }
    public int RemoveCallCount { get; private set; }

    public void Seed(UserId userId, string pseudonym, DateTime joinedAtUtc) =>
        _stored.Add(new StoredProfile(userId, Pseudonym.Create(pseudonym).Value, joinedAtUtc));

    public Task<LeaderboardProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var stored = _stored.FirstOrDefault(p => p.UserId == userId);

        return Task.FromResult(stored is null
            ? null
            : LeaderboardProfile.Join(stored.UserId, stored.Pseudonym, stored.JoinedAtUtc));
    }

    public Task<bool> IsPseudonymTakenAsync(Pseudonym pseudonym, CancellationToken cancellationToken = default)
    {
        CheckedCandidates.Add(pseudonym);

        var taken = CheckedCandidates.Count <= TakenCandidateCount
                    || _stored.Any(p => p.Pseudonym == pseudonym);

        return Task.FromResult(taken);
    }

    public Task<bool> TryAddAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        TryAddCallCount++;
        OnTryAdd?.Invoke(profile);

        if (_stored.Any(p => p.UserId == profile.Id || p.Pseudonym == profile.Pseudonym))
            return Task.FromResult(false);

        _stored.Add(new StoredProfile(profile.Id, profile.Pseudonym, profile.JoinedAtUtc));
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdateAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        TryUpdateCallCount++;
        OnTryUpdate?.Invoke(profile);

        if (_stored.Any(p => p.UserId != profile.Id && p.Pseudonym == profile.Pseudonym))
            return Task.FromResult(false);

        var index = _stored.FindIndex(p => p.UserId == profile.Id);
        _stored[index] = _stored[index] with { Pseudonym = profile.Pseudonym };
        return Task.FromResult(true);
    }

    public Task RemoveAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        RemoveCallCount++;
        _stored.RemoveAll(p => p.UserId == profile.Id);
        return Task.CompletedTask;
    }

    public sealed record StoredProfile(UserId UserId, Pseudonym Pseudonym, DateTime JoinedAtUtc);
}
