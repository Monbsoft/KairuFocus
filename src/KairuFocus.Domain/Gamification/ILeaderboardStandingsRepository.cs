namespace KairuFocus.Domain.Gamification;

/// <summary>
/// CROSS-USERS read (ADR-024) — community members only. No userId parameter.
/// Projection (Name, AvatarUrl, Xp): nothing else leaves, not even a UserId. Never reads Users.
/// </summary>
public interface ILeaderboardStandingsRepository
{
    /// <summary>XP of each member within [startUtc, endUtc). A member without gain has no row.</summary>
    Task<IReadOnlyList<XpStanding>> GetStandingsBetweenAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>Total XP of each member.</summary>
    Task<IReadOnlyList<XpStanding>> GetAllTimeStandingsAsync(CancellationToken cancellationToken = default);
}
