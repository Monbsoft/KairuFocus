using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

/// <summary>
/// Writes of a member profile and read of one's own profile. The Try* methods return false instead of
/// throwing when the store rejects the change: unique name or user taken concurrently, row deleted
/// concurrently.
/// </summary>
public interface IMemberProfileRepository
{
    Task<MemberProfile?> GetByUserIdAsync(UserId userId, CancellationToken ct = default);

    /// <summary>Compares on <see cref="ProfileName.Key"/>.</summary>
    Task<bool> IsNameTakenAsync(ProfileName name, CancellationToken ct = default);

    Task<bool> TryAddAsync(MemberProfile profile, CancellationToken ct = default);
    Task<bool> TryUpdateAsync(MemberProfile profile, CancellationToken ct = default);
    Task<bool> TryRemoveAsync(MemberProfile profile, CancellationToken ct = default);
}
