namespace KairuFocus.Domain.Community;

/// <summary>Reads across users, by name only: no user identifier enters or leaves this contract.</summary>
public interface IMemberDirectoryReadRepository
{
    Task<PublicMemberProfile?> FindByNameAsync(ProfileName name, CancellationToken ct = default);
    Task<MemberActivity> GetActivityAsync(ProfileName name, DateTime heatmapSinceUtc, CancellationToken ct = default);
}
