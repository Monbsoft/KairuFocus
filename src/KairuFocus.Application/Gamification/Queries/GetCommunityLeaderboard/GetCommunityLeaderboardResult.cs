namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

/// <summary>
/// Leaderboard of the period. The only data about other members is rank, profile name, avatar (GitHub
/// names only) and period XP: no user identifier, email or display name (ADR-024).
/// </summary>
/// <param name="IsParticipant">The current user has a community profile.</param>
/// <param name="RankedCount">Members with more than 0 XP over the period.</param>
/// <param name="Me">Null for a non-member or a member with 0 XP over the period.</param>
/// <param name="Top">At most 10 rows.</param>
public sealed record GetCommunityLeaderboardResult(
    bool IsParticipant,
    int RankedCount,
    MyStandingViewModel? Me,
    IReadOnlyList<LeaderboardRowViewModel> Top);
