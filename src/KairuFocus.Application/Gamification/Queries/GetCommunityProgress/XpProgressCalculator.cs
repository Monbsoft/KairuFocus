using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>
/// Pure computation (no I/O) of the XP part of the progression: period XP, comparison at the same
/// moment, weekly average, records and chart bars. Sprint figures (<c>Sprints</c>, <c>FocusMinutes</c>,
/// <c>AllTime.Sprints</c>) are left at 0 here and filled by the handler from the sprint intervals.
/// </summary>
internal static class XpProgressCalculator
{
    public const int WeeklyBarCount = 8;

    public static GetCommunityProgressResult ComputeXp(
        IReadOnlyList<XpEarning> earnings,
        CommunityPeriod period,
        DateTime utcNow)
    {
        // EF Core reads datetime2 as DateTimeKind.Unspecified: pin every instant to UTC once, so that
        // the DateTimeOffset conversions below never interpret it as local time.
        var gains = earnings
            .Select(e => e with { EarnedAtUtc = AsUtc(e.EarnedAtUtc) })
            .ToList();

        DateTime? firstGain = gains.Count > 0 ? gains.Min(g => g.EarnedAtUtc) : null;
        var totalXp = gains.Sum(g => g.Amount);
        var window = CommunityCalendar.CurrentWindow(period, utcNow, firstGain);
        var xp = window is { } current ? SumWithin(gains, current) : 0;

        var bestWeek = BestPeriod(gains, CommunityCalendar.WeekStart);
        var bestMonth = BestPeriod(gains, CommunityCalendar.MonthStart);

        return new GetCommunityProgressResult(
            WindowStart: window is { } start ? DateOnly.FromDateTime(start.StartUtc) : null,
            Xp: xp,
            Sprints: 0,
            FocusMinutes: 0,
            Comparison: period == CommunityPeriod.Global ? null : Compare(gains, period, utcNow, xp),
            AverageWeeklyXp: period == CommunityPeriod.Global ? AverageWeeklyXp(totalXp, firstGain, utcNow) : null,
            AllTime: new AllTimeViewModel(totalXp, Sprints: 0, FirstGainAt: firstGain is { } first ? ToInstant(first) : null),
            BestWeek: bestWeek,
            BestMonth: bestMonth,
            Bars: period switch
            {
                CommunityPeriod.Week => WeeklyBars(gains, utcNow, bestWeek),
                CommunityPeriod.Month => MonthlyBars(gains, utcNow, firstGain, bestMonth),
                CommunityPeriod.Global => CumulativeBars(gains, utcNow, firstGain, totalXp),
                _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown community period."),
            });
    }

    private static XpComparisonViewModel Compare(
        IReadOnlyList<XpEarning> gains, CommunityPeriod period, DateTime utcNow, int xp)
    {
        var previous = CommunityCalendar.PreviousSameMoment(period, utcNow);
        var previousXp = SumWithin(gains, previous);

        return new XpComparisonViewModel(previousXp, ToInstant(previous.EndUtc), xp - previousXp);
    }

    /// <summary>round(total / UTC calendar weeks since the week of the first gain, current week included).</summary>
    private static int? AverageWeeklyXp(int totalXp, DateTime? firstGain, DateTime utcNow)
    {
        if (firstGain is not { } first)
            return null;

        var weeks = CommunityCalendar.WeeksSpanned(first, utcNow);
        return (int)Math.Round((decimal)totalXp / weeks, MidpointRounding.AwayFromZero);
    }

    /// <summary>Period (week or month) with the most XP, current one included. On a tie, the earliest keeps the record.</summary>
    private static XpRecordViewModel? BestPeriod(IReadOnlyList<XpEarning> gains, Func<DateTime, DateTime> periodStart)
    {
        XpRecordViewModel? best = null;

        foreach (var period in gains.GroupBy(g => periodStart(g.EarnedAtUtc)).OrderBy(p => p.Key))
        {
            var xp = period.Sum(g => g.Amount);
            if (best is null || xp > best.Xp)
                best = new XpRecordViewModel(DateOnly.FromDateTime(period.Key), xp);
        }

        return best;
    }

    /// <summary>The last 8 UTC weeks, current one included, empty weeks at 0.</summary>
    private static List<XpBarViewModel> WeeklyBars(
        IReadOnlyList<XpEarning> gains, DateTime utcNow, XpRecordViewModel? bestWeek)
    {
        var currentWeek = CommunityCalendar.WeekStart(utcNow);
        var bars = new List<XpBarViewModel>(WeeklyBarCount);

        for (var weeksAgo = WeeklyBarCount - 1; weeksAgo >= 0; weeksAgo--)
        {
            var start = currentWeek.AddDays(-7 * weeksAgo);
            var periodStart = DateOnly.FromDateTime(start);

            bars.Add(new XpBarViewModel(
                periodStart,
                SumWithin(gains, new UtcRange(start, start.AddDays(7))),
                IsCurrent: weeksAgo == 0,
                IsRecord: bestWeek?.PeriodStart == periodStart));
        }

        return bars;
    }

    /// <summary>One bar per UTC month from the month of the first gain to the current one (only the current one without any gain).</summary>
    private static List<XpBarViewModel> MonthlyBars(
        IReadOnlyList<XpEarning> gains, DateTime utcNow, DateTime? firstGain, XpRecordViewModel? bestMonth)
    {
        var currentMonth = CommunityCalendar.MonthStart(utcNow);
        var firstMonth = firstGain is { } first && CommunityCalendar.MonthStart(first) < currentMonth
            ? CommunityCalendar.MonthStart(first)
            : currentMonth;

        var bars = new List<XpBarViewModel>();
        for (var month = firstMonth; month <= currentMonth; month = month.AddMonths(1))
        {
            var periodStart = DateOnly.FromDateTime(month);

            bars.Add(new XpBarViewModel(
                periodStart,
                SumWithin(gains, new UtcRange(month, month.AddMonths(1))),
                IsCurrent: month == currentMonth,
                IsRecord: bestMonth?.PeriodStart == periodStart));
        }

        return bars;
    }

    /// <summary>
    /// Cumulative XP at the end of each UTC month, from the month of the first gain to the previous month,
    /// then a last bar holding the total so far ("today"), labelled with the current month. No record mark.
    /// </summary>
    private static List<XpBarViewModel> CumulativeBars(
        IReadOnlyList<XpEarning> gains, DateTime utcNow, DateTime? firstGain, int totalXp)
    {
        if (firstGain is not { } first)
            return [];

        var currentMonth = CommunityCalendar.MonthStart(utcNow);
        var bars = new List<XpBarViewModel>();

        for (var month = CommunityCalendar.MonthStart(first); month < currentMonth; month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1);

            bars.Add(new XpBarViewModel(
                DateOnly.FromDateTime(month),
                gains.Where(g => g.EarnedAtUtc < monthEnd).Sum(g => g.Amount),
                IsCurrent: false,
                IsRecord: false));
        }

        bars.Add(new XpBarViewModel(DateOnly.FromDateTime(currentMonth), totalXp, IsCurrent: true, IsRecord: false));

        return bars;
    }

    private static int SumWithin(IReadOnlyList<XpEarning> gains, UtcRange range) =>
        gains.Where(g => range.Contains(g.EarnedAtUtc)).Sum(g => g.Amount);

    private static DateTime AsUtc(DateTime instant) => DateTime.SpecifyKind(instant, DateTimeKind.Utc);

    private static DateTimeOffset ToInstant(DateTime utc) => new(AsUtc(utc));
}
