namespace KairuFocus.Application.Pomodoro.Common;

/// <summary>
/// Encapsulates the UTC range that corresponds to "today" in the user's local timezone,
/// derived from a UTC instant and a client-supplied UTC offset (minutes).
/// </summary>
internal readonly record struct LocalDayWindow(
    DateOnly TodayLocal,
    DateTime StartUtc,
    DateTime EndUtc,
    TimeSpan Offset)
{
    /// <summary>
    /// Valid UTC offset range: −12:00 (−720 min) to +14:00 (+840 min).
    /// Values outside this range are clamped to prevent arithmetic exploits.
    /// </summary>
    private const int MinOffsetMinutes = -840;
    private const int MaxOffsetMinutes = 840;

    /// <summary>
    /// Computes the local-day window from a UTC "now" instant and an offset supplied by the client.
    /// Convention: local = UTC + offsetMinutes.
    /// </summary>
    public static LocalDayWindow From(DateTime utcNow, int offsetMinutes)
    {
        var clampedOffset = Math.Clamp(offsetMinutes, MinOffsetMinutes, MaxOffsetMinutes);
        var offset = TimeSpan.FromMinutes(clampedOffset);
        var nowLocal = utcNow + offset;               // ticks shifted to represent local clock
        var todayLocal = DateOnly.FromDateTime(nowLocal);
        var startUtc = nowLocal.Date - offset;        // UTC instant of local midnight today
        var endUtc = startUtc + TimeSpan.FromDays(1);
        return new LocalDayWindow(todayLocal, startUtc, endUtc, offset);
    }
}
