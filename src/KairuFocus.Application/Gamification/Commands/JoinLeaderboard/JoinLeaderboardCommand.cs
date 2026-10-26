using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Gamification.Commands.JoinLeaderboard;

/// <summary>UC-XP-03 — the current user joins the community leaderboard under a generated pseudonym.</summary>
public sealed record JoinLeaderboardCommand : ICommand<JoinLeaderboardResult>;
