namespace KairuFocus.Domain.Community;

/// <summary>When a member counts as connected.</summary>
public static class PresenceRules
{
    /// <summary>A member is present while their last sighting is at most this old.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(3);

    /// <summary>Oldest last-sighting still counted as present at <paramref name="nowUtc"/> (inclusive bound).</summary>
    public static DateTime SeenSince(DateTime nowUtc) => nowUtc - Window;
}
