namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

public sealed record LeaderboardRowViewModel(int Rank, string Pseudonym, int Xp, bool IsMe);
