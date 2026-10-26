namespace KairuFocus.Domain.Gamification;

/// <summary>Projection of one XP gain of a single user (read side of the ledger).</summary>
public sealed record XpEarning(DateTime EarnedAtUtc, int Amount);
