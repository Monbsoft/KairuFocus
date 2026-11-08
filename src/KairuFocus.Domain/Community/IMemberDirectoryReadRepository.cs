namespace KairuFocus.Domain.Community;

/// <summary>Reads across users, by name only: no user identifier enters or leaves this contract.</summary>
public interface IMemberDirectoryReadRepository
{
    Task<PublicMemberProfile?> FindByNameAsync(ProfileName name, CancellationToken ct = default);
    Task<MemberActivity> GetActivityAsync(ProfileName name, DateTime heatmapSinceUtc, CancellationToken ct = default);

    /// <summary>
    /// Activated members with at least one completed sprint ended in [<paramref name="fromUtc"/>, <paramref name="toUtc"/>[,
    /// most recent end first then by name (ordinal), at most <paramref name="maxMembers"/>; Total counts every distinct member.
    /// </summary>
    Task<ActiveMembers> GetActiveMembersAsync(DateTime fromUtc, DateTime toUtc, int maxMembers, CancellationToken ct = default);
}
