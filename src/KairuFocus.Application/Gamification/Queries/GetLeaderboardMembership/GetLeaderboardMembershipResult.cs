namespace KairuFocus.Application.Gamification.Queries.GetLeaderboardMembership;

/// <summary>Pseudonym is null for a non-participant.</summary>
public sealed record GetLeaderboardMembershipResult(bool IsParticipant, string? Pseudonym);
