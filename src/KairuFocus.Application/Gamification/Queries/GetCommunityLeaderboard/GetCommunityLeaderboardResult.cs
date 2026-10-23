namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

/// <summary>
/// Leaderboard of the period. The only data about other members is rank, pseudonym and period XP:
/// no user identifier, login, name, email or avatar (ADR-024).
/// </summary>
/// <param name="RankedCount">Participants with more than 0 XP over the period.</param>
/// <param name="Me">Null for a non-participant or a participant with 0 XP over the period.</param>
/// <param name="Top">At most 10 rows.</param>
public sealed record GetCommunityLeaderboardResult(
    bool IsParticipant,
    int RankedCount,
    MyStandingViewModel? Me,
    IReadOnlyList<LeaderboardRowViewModel> Top);
