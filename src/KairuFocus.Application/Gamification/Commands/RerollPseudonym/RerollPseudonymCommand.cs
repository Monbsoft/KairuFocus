using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Gamification.Commands.RerollPseudonym;

/// <summary>UC-XP-04 — the current participant draws another pseudonym. Unlimited; XP and rank are unchanged.</summary>
public sealed record RerollPseudonymCommand : ICommand<RerollPseudonymResult>;
