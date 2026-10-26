using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetLeaderboardMembership;

/// <summary>UC-XP-06 — participation status of the current user. The pseudonym is only returned to its owner.</summary>
public sealed record GetLeaderboardMembershipQuery : IQuery<GetLeaderboardMembershipResult>;
