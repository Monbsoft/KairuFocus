using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Gamification.Commands.LeaveLeaderboard;

/// <summary>UC-XP-05 — the current participant leaves the leaderboard. The XP ledger is never touched.</summary>
public sealed record LeaveLeaderboardCommand : ICommand<LeaveLeaderboardResult>;
