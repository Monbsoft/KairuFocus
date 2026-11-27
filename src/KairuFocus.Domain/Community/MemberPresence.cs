using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

/// <summary>
/// Last time a member was seen connected, keyed by <see cref="UserId"/>. See <see cref="PresenceRules"/> for
/// the window during which this counts as present.
/// </summary>
public sealed class MemberPresence : AggregateRoot<UserId>
{
    // Parameter names match property names: EF Core materializes via constructor binding.
    private MemberPresence(UserId id, DateTime lastSeenAtUtc) : base(id) => LastSeenAtUtc = lastSeenAtUtc;

    public DateTime LastSeenAtUtc { get; private set; }

    /// <summary>Guards throw: invalid arguments are programmer errors.</summary>
    public static MemberPresence Record(UserId userId, DateTime seenAtUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);
        if (seenAtUtc == default)
            throw new ArgumentException("SeenAtUtc must be a valid UTC instant.", nameof(seenAtUtc));

        return new MemberPresence(userId, seenAtUtc);
    }

    /// <summary>Moves the last sighting forward; an older instant (late or reordered heartbeat) is ignored.</summary>
    public void SeenAt(DateTime seenAtUtc)
    {
        if (seenAtUtc > LastSeenAtUtc)
            LastSeenAtUtc = seenAtUtc;
    }
}
