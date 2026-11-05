namespace KairuFocus.Domain.Community;

/// <summary>What any connected member may read of another member. Carries no user identifier.</summary>
public sealed record PublicMemberProfile(ProfileName Name, ProfileNameSource NameSource, string? AvatarUrl, DateTime JoinedAtUtc);
