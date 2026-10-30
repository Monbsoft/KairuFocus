using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.LeaveCommunity;

/// <summary>UC-CO-03 — the current member leaves the community. The XP ledger is never touched.</summary>
public sealed record LeaveCommunityCommand : ICommand<LeaveCommunityResult>;
