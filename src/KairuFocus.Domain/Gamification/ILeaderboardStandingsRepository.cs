namespace KairuFocus.Domain.Gamification;

/// <summary>
/// CROSS-USERS read (ADR-024) — opt-in participants only. No userId parameter.
/// Projection (Pseudonym, Xp): nothing else leaves, not even a UserId. Never reads Users.
/// </summary>
public interface ILeaderboardStandingsRepository
{
    /// <summary>XP of each participant within [startUtc, endUtc). A participant without gain has no row.</summary>
    Task<IReadOnlyList<XpStanding>> GetStandingsBetweenAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>Total XP of each participant.</summary>
    Task<IReadOnlyList<XpStanding>> GetAllTimeStandingsAsync(CancellationToken cancellationToken = default);
}
