using KairuFocus.Application.Gamification.Common;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

/// <summary>UC-XP-08 — leaderboard of the opt-in participants for the period (total XP for global).</summary>
public sealed record GetCommunityLeaderboardQuery(CommunityPeriod Period) : IQuery<GetCommunityLeaderboardResult>;
