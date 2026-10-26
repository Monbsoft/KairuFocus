namespace KairuFocus.Application.Gamification.Common;

/// <summary>
/// UTC calendar rules of the community page (ADR-022, ADR-024): a common window for every member.
/// Week = [Monday 00:00 UTC, next Monday); month = [1st 00:00 UTC, 1st of next month).
/// Pure: the caller provides the instant.
/// </summary>
internal static class CommunityCalendar
{
    public static DateTime WeekStart(DateTime utc) => UtcWeekWindow.From(utc).StartUtc;

    public static DateTime MonthStart(DateTime utc) => new(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Window of the current period. Global = [firstGainUtc, DateTime.MaxValue), open-ended since
    /// every EndedAt / EarnedAtUtc is at most now; null when there is no gain.
    /// </summary>
    public static UtcRange? CurrentWindow(CommunityPeriod period, DateTime utcNow, DateTime? firstGainUtc)
    {
        switch (period)
        {
            case CommunityPeriod.Week:
                var weekStart = WeekStart(utcNow);
                return new UtcRange(weekStart, weekStart.AddDays(7));

            case CommunityPeriod.Month:
                var monthStart = MonthStart(utcNow);
                return new UtcRange(monthStart, monthStart.AddMonths(1));

            case CommunityPeriod.Global:
                return firstGainUtc is { } firstGain ? new UtcRange(firstGain, DateTime.MaxValue) : null;

            default:
                throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown community period.");
        }
    }

    /// <summary>
    /// Previous period, from its start up to the same moment.
    /// Week: [Monday − 7 d, now − 7 d). Month: [first day of previous month, now − 1 month).
    /// DateTime.AddMonths clamps a missing day to the last day of the month (31/10 → 30/09,
    /// 31/03 → 28/02 or 29/02): exactly the "fall back on the end of the previous month" rule.
    /// Global has no previous period.
    /// </summary>
    public static UtcRange PreviousSameMoment(CommunityPeriod period, DateTime utcNow) => period switch
    {
        CommunityPeriod.Week => new UtcRange(WeekStart(utcNow).AddDays(-7), utcNow.AddDays(-7)),
        CommunityPeriod.Month => new UtcRange(MonthStart(utcNow).AddMonths(-1), utcNow.AddMonths(-1)),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Only week and month have a previous period."),
    };

    /// <summary>UTC calendar weeks from the week of <paramref name="fromUtc"/> to the week of <paramref name="toUtc"/>, both included (≥ 1).</summary>
    public static int WeeksSpanned(DateTime fromUtc, DateTime toUtc)
    {
        var weeks = (WeekStart(toUtc) - WeekStart(fromUtc)).Days / 7 + 1;
        return Math.Max(1, weeks);
    }
}
