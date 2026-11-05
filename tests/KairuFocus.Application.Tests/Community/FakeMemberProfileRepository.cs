using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Community;

/// <summary>
/// In-memory member profiles. Stores snapshots, like a database: a profile returned by
/// <see cref="GetByUserIdAsync"/> is a fresh copy, and a rejected update leaves the stored row unchanged.
/// Simulates the primary key (UserId), the unique index on the name key, and the concurrency check of an
/// UPDATE / DELETE that affects no row (profile deleted concurrently).
/// </summary>
internal sealed class FakeMemberProfileRepository : IMemberProfileRepository
{
    private readonly List<StoredProfile> _stored = [];

    public IReadOnlyList<StoredProfile> Stored => _stored;

    /// <summary>Thrown by <see cref="GetByUserIdAsync"/> and <see cref="IsNameTakenAsync"/> when set.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>When true, <see cref="TryAddAsync"/> stores nothing and returns false, whatever the stored rows.</summary>
    public bool RejectTryAdd { get; set; }

    /// <summary>Every name passed to <see cref="IsNameTakenAsync"/>, in order.</summary>
    public List<ProfileName> CheckedNames { get; } = [];

    /// <summary>
    /// Invoked at the beginning of IsNameTakenAsync with the name: lets a test change the stored rows
    /// between the read of the profile and the check, as a concurrent request would.
    /// </summary>
    public Action<ProfileName>? OnIsNameTaken { get; set; }

    /// <summary>
    /// Invoked at the beginning of TryAddAsync with the profile being inserted: lets a test insert the
    /// concurrent "winner" (same user, or another user with the same name).
    /// </summary>
    public Action<MemberProfile>? OnTryAdd { get; set; }

    /// <summary>
    /// Invoked at the beginning of TryUpdateAsync: lets a test take the new name concurrently, or
    /// delete the profile concurrently (<see cref="Delete"/>).
    /// </summary>
    public Action<MemberProfile>? OnTryUpdate { get; set; }

    /// <summary>Invoked at the beginning of TryRemoveAsync: lets a test delete the profile concurrently.</summary>
    public Action<MemberProfile>? OnTryRemove { get; set; }

    public int TryAddCallCount { get; private set; }
    public int TryUpdateCallCount { get; private set; }
    public int TryRemoveCallCount { get; private set; }

    /// <summary>Stores a row directly (does not count as a call). Goes through the invariants of <see cref="MemberProfile.Join"/>.</summary>
    public void Seed(
        UserId userId,
        string name,
        ProfileNameSource source = ProfileNameSource.Chosen,
        string? avatarUrl = null,
        DateTime? joinedAtUtc = null) =>
        _stored.Add(StoredProfile.From(MemberProfile.Join(
            userId,
            ProfileName.Create(name).Value,
            source,
            avatarUrl,
            joinedAtUtc ?? new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc))));

    /// <summary>Deletes a stored row directly, as a concurrent request would (does not count as a call).</summary>
    public void Delete(UserId userId) => _stored.RemoveAll(p => p.UserId == userId);

    public Task<MemberProfile?> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        var stored = _stored.FirstOrDefault(p => p.UserId == userId);

        return Task.FromResult(stored is null
            ? null
            : MemberProfile.Join(stored.UserId, stored.Name, stored.NameSource, stored.AvatarUrl, stored.JoinedAtUtc));
    }

    public Task<bool> IsNameTakenAsync(ProfileName name, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        CheckedNames.Add(name);
        OnIsNameTaken?.Invoke(name);

        return Task.FromResult(_stored.Any(p => p.Name.Key == name.Key));
    }

    public Task<bool> TryAddAsync(MemberProfile profile, CancellationToken ct = default)
    {
        TryAddCallCount++;
        OnTryAdd?.Invoke(profile);

        if (RejectTryAdd || _stored.Any(p => p.UserId == profile.Id || p.Name.Key == profile.Name.Key))
            return Task.FromResult(false);

        _stored.Add(StoredProfile.From(profile));
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdateAsync(MemberProfile profile, CancellationToken ct = default)
    {
        TryUpdateCallCount++;
        OnTryUpdate?.Invoke(profile);

        // The UPDATE affects no row when the profile was deleted concurrently.
        var index = _stored.FindIndex(p => p.UserId == profile.Id);
        if (index < 0)
            return Task.FromResult(false);

        if (_stored.Any(p => p.UserId != profile.Id && p.Name.Key == profile.Name.Key))
            return Task.FromResult(false);

        _stored[index] = StoredProfile.From(profile);
        return Task.FromResult(true);
    }

    public Task<bool> TryRemoveAsync(MemberProfile profile, CancellationToken ct = default)
    {
        TryRemoveCallCount++;
        OnTryRemove?.Invoke(profile);

        // The DELETE affects no row when the profile was already deleted concurrently.
        return Task.FromResult(_stored.RemoveAll(p => p.UserId == profile.Id) > 0);
    }

    private void ThrowIfReadFails()
    {
        if (ThrowOnRead is not null)
            throw ThrowOnRead;
    }

    public sealed record StoredProfile(
        UserId UserId,
        ProfileName Name,
        ProfileNameSource NameSource,
        string? AvatarUrl,
        DateTime JoinedAtUtc)
    {
        public static StoredProfile From(MemberProfile profile) =>
            new(profile.Id, profile.Name, profile.NameSource, profile.AvatarUrl, profile.JoinedAtUtc);
    }
}
