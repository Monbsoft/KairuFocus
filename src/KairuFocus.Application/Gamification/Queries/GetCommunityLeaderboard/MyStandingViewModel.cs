namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

/// <summary>The current user's row. IsTied: another member has the same XP. IsInTop: the row is part of Top.</summary>
public sealed record MyStandingViewModel(int Rank, bool IsTied, int Xp, string Pseudonym, bool IsInTop);
