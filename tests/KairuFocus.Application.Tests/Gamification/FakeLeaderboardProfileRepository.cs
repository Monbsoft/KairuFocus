using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Gamification;

/// <summary>
/// In-memory leaderboard profiles. Stores snapshots, like a database: a profile returned by
/// <see cref="GetByUserIdAsync"/> is a fresh copy, and a rejected update leaves the stored row unchanged.
/// Simulates the primary key (UserId), the unique pseudonym index, and the concurrency check of an
/// UPDATE / DELETE that affects no row (profile deleted concurrently).
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
    /// Invoked at the beginning of IsPseudonymTakenAsync with the candidate: lets a test change the stored
    /// rows between the read of the profile and the draw, as a concurrent reroll or leave (another tab) would.
    /// </summary>
    public Action<Pseudonym>? OnIsPseudonymTaken { get; set; }

    /// <summary>
    /// Invoked at the beginning of TryAddAsync with the profile being inserted: lets a test insert the
    /// concurrent "winner" (same user, or another user with the same pseudonym).
    /// </summary>
    public Action<LeaderboardProfile>? OnTryAdd { get; set; }

    /// <summary>
    /// Invoked at the beginning of TryUpdateAsync: lets a test take the new pseudonym concurrently, or
    /// delete the profile concurrently (<see cref="Delete"/>).
    /// </summary>
    public Action<LeaderboardProfile>? OnTryUpdate { get; set; }

    /// <summary>Invoked at the beginning of TryRemoveAsync: lets a test delete the profile concurrently.</summary>
    public Action<LeaderboardProfile>? OnTryRemove { get; set; }

    public int TryAddCallCount { get; private set; }
    public int TryUpdateCallCount { get; private set; }
    public int TryRemoveCallCount { get; private set; }

    public void Seed(UserId userId, string pseudonym, DateTime joinedAtUtc) =>
        _stored.Add(new StoredProfile(userId, Pseudonym.Create(pseudonym).Value, joinedAtUtc));

    /// <summary>Deletes a stored row directly, as a concurrent request would (does not count as a call).</summary>
    public void Delete(UserId userId) => _stored.RemoveAll(p => p.UserId == userId);

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
        OnIsPseudonymTaken?.Invoke(pseudonym);

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

        // The UPDATE affects no row when the profile was deleted concurrently.
        var index = _stored.FindIndex(p => p.UserId == profile.Id);
        if (index < 0)
            return Task.FromResult(false);

        if (_stored.Any(p => p.UserId != profile.Id && p.Pseudonym == profile.Pseudonym))
            return Task.FromResult(false);

        _stored[index] = _stored[index] with { Pseudonym = profile.Pseudonym };
        return Task.FromResult(true);
    }

    public Task<bool> TryRemoveAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        TryRemoveCallCount++;
        OnTryRemove?.Invoke(profile);

        // The DELETE affects no row when the profile was already deleted concurrently.
        return Task.FromResult(_stored.RemoveAll(p => p.UserId == profile.Id) > 0);
    }

    public sealed record StoredProfile(UserId UserId, Pseudonym Pseudonym, DateTime JoinedAtUtc);
}
