namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

public sealed record LeaderboardRowViewModel(int Rank, string Name, string? AvatarUrl, int Xp, bool IsMe);
