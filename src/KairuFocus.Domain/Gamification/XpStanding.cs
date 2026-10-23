namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Cross-user projection of a leaderboard participant (ADR-024): pseudonym and XP only,
/// never a user identifier.
/// </summary>
public sealed record XpStanding(Pseudonym Pseudonym, int Xp);
