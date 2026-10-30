using KairuFocus.Domain.Community;

namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Cross-user projection of a community member in the standings (ADR-024): public profile name, avatar
/// (GitHub names only) and XP, never a user identifier.
/// </summary>
public sealed record XpStanding(ProfileName Name, string? AvatarUrl, int Xp);
