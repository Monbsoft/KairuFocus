namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Per-sprint rules of the community page (founder decision 21, ADR-024). Pure: no clock,
/// no I/O. Applied row by row by the Application (the user's own sprints) and by the
/// Infrastructure (anonymous cross-user projections), so there is a single definition.
/// Comparisons only use ticks: the <see cref="DateTimeKind"/> of the arguments is ignored.
/// </summary>
public static class CommunityRules
{
    public static readonly TimeSpan MaxCountedSprintDuration = TimeSpan.FromHours(4);

    /// <summary>Focus counted for a completed sprint: clamp(endedAt − startedAt, 0, 4 h).</summary>
    public static TimeSpan CountedFocus(DateTime startedAt, DateTime endedAt)
    {
        var duration = endedAt - startedAt;

        if (duration <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return duration < MaxCountedSprintDuration ? duration : MaxCountedSprintDuration;
    }

    /// <summary>
    /// Whether an active sprint counts as "in progress" at <paramref name="nowUtc"/>.
    /// End of presence = startedAt + (planned &gt; 0 ? min(planned, 4 h) : 4 h); in progress ⇔ nowUtc &lt; end.
    /// No lower bound: a startedAt slightly after nowUtc (clock skew between instances) counts.
    /// Status (Active) and type (Sprint) are filtered by the caller.
    /// </summary>
    public static bool IsSprintInProgress(DateTime startedAt, int plannedDurationMinutes, DateTime nowUtc)
    {
        var presence = MaxCountedSprintDuration;

        if (plannedDurationMinutes > 0)
        {
            var planned = TimeSpan.FromMinutes(plannedDurationMinutes);
            if (planned < presence)
                presence = planned;
        }

        return nowUtc < startedAt + presence;
    }
}
