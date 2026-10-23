using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Opt-in participation in the community leaderboard (ADR-024). One profile per user,
/// keyed by <see cref="UserId"/> (same pattern as UserSettings). Leaving the leaderboard
/// deletes the profile; the XP ledger is never touched.
/// </summary>
public sealed class LeaderboardProfile : AggregateRoot<UserId>
{
    // Parameter names match property names: EF Core materializes via constructor binding
    // (same pattern as XpGain).
    private LeaderboardProfile(UserId id, Pseudonym pseudonym, DateTime joinedAtUtc)
        : base(id)
    {
        Pseudonym = pseudonym;
        JoinedAtUtc = joinedAtUtc;
    }

    public Pseudonym Pseudonym { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    /// <summary>
    /// Creates the profile of a user joining the leaderboard. Guards throw: invalid arguments
    /// are programmer errors (pseudonym uniqueness is checked by the use case), not part of
    /// the normal domain flow.
    /// </summary>
    public static LeaderboardProfile Join(UserId userId, Pseudonym pseudonym, DateTime joinedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(pseudonym);
        if (joinedAtUtc == default)
            throw new ArgumentException("JoinedAtUtc must be a valid UTC instant.", nameof(joinedAtUtc));

        return new LeaderboardProfile(userId, pseudonym, joinedAtUtc);
    }

    /// <summary>
    /// Replaces the pseudonym. Guards throw: the reroll use case explicitly excludes the current pseudonym
    /// from the draw (the store alone cannot, a concurrent reroll or leave may have freed it), so receiving
    /// it again is a programmer error, never a concurrency outcome.
    /// </summary>
    public void ChangePseudonym(Pseudonym newPseudonym)
    {
        ArgumentNullException.ThrowIfNull(newPseudonym);
        if (newPseudonym == Pseudonym)
            throw new ArgumentException("The new pseudonym must differ from the current one.", nameof(newPseudonym));

        Pseudonym = newPseudonym;
    }
}
