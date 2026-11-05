namespace KairuFocus.Application.Pomodoro.Common;

/// <summary>
/// Time window during which a start request is treated as a re-submission of the same user action
/// (double-click, retry after a slow response) instead of a conflict. Kept deliberately short: beyond
/// it, an already active session is a genuine conflict, not a duplicate click.
/// </summary>
internal static class SessionStartIdempotency
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <paramref name="now"/> must be sampled after the read of the session: the row may have been
    /// inserted while that read was in flight, which would make its age negative against an older
    /// timestamp. Lower bound: with clock drift between instances, a start in the future would otherwise
    /// always look like a re-submission, whatever its age.
    /// </summary>
    public static bool IsResubmission(DateTime startedAt, DateTime now)
    {
        var elapsed = now - startedAt;
        return elapsed >= TimeSpan.Zero && elapsed <= Window;
    }
}
