namespace KairuFocus.Domain.Community;

/// <summary>A member taking part in an open sprint, as shown to the other members.</summary>
public sealed record SprintMember(ProfileName Name, string? AvatarUrl);
