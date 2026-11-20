using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

/// <summary>
/// Writes and own-group reads of group sessions. The Try* methods return false instead of throwing when
/// the store rejects the change.
/// </summary>
public interface IGroupSessionRepository
{
    Task<GroupSession?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The most recent group the user hosts or belongs to (member without <see cref="GroupMember.LeftAtUtc"/>),
    /// ended or not. A group whose end is not recorded comes before any ended group, then the latest step first.</summary>
    Task<GroupSession?> FindLatestForUserAsync(UserId userId, CancellationToken ct = default);

    /// <summary>False when the id already exists.</summary>
    Task<bool> TryAddAsync(GroupSession group, CancellationToken ct = default);

    /// <summary>False on a <see cref="GroupSession.Version"/> conflict.</summary>
    Task<bool> TryUpdateAsync(GroupSession group, CancellationToken ct = default);
}
