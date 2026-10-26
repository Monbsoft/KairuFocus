namespace KairuFocus.Application.Gamification.Queries.GetCommunityRecap;

/// <summary>Four numbers only: no identifier, no pseudonym, no row. MySprints is counted on the same window (≤ Sprints).</summary>
public sealed record GetCommunityRecapResult(int Sprints, int FocusMinutes, int ActiveMembers, int MySprints);
