namespace KairuFocus.Domain.Gamification;

/// <summary>
/// CROSS-USERS read (ADR-024) — all members, participants or not. Anonymous aggregates only:
/// counts, sums, one date. No row, no identifier, no pseudonym. Never reads Users.
/// Lives in Gamification (the consumer defines the port); Pomodoro learns nothing about it.
/// </summary>
public interface ICommunityActivityRepository
{
    /// <summary>Active sprints for which <see cref="CommunityRules.IsSprintInProgress"/>(…, nowUtc) is true.</summary>
    Task<int> CountSprintsInProgressAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Distinct members who completed at least one sprint with EndedAt within [startUtc, endUtc).</summary>
    Task<int> CountActiveMembersAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>Number of completed sprints and sum of <see cref="CommunityRules.CountedFocus"/> within [startUtc, endUtc).</summary>
    Task<CompletedSprintTotals> GetCompletedSprintTotalsAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>Oldest EarnedAtUtc of the ledger, all users: when XP went live. Null when the ledger is empty.</summary>
    Task<DateTime?> GetFirstXpGainUtcAsync(CancellationToken cancellationToken = default);
}
