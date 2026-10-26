using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityProgress;
using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class XpProgressCalculatorTests
{
    // Thursday 2026-10-22 14:00 UTC → week [Mon 2026-10-19, Mon 2026-10-26), month [2026-10-01, 2026-11-01).
    private static readonly DateTime Now = Utc(2026, 10, 22, 14);

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static XpEarning Gain(DateTime earnedAtUtc, int amount = 10) => new(earnedAtUtc, amount);

    private static GetCommunityProgressResult Compute(CommunityPeriod period, params XpEarning[] earnings) =>
        XpProgressCalculator.ComputeXp(earnings.OrderBy(e => e.EarnedAtUtc).ToList(), period, Now);

    [Fact]
    public void Should_IncludeGainAtWindowStart_When_SummingPeriodXp()
    {
        var result = Compute(CommunityPeriod.Week, Gain(Utc(2026, 10, 19)));

        Assert.Equal(10, result.Xp);
        Assert.Equal(new DateOnly(2026, 10, 19), result.WindowStart);
    }

    [Fact]
    public void Should_ExcludeGainBeforeWindowStart_When_SummingPeriodXp()
    {
        var result = Compute(CommunityPeriod.Week, Gain(Utc(2026, 10, 18, 23, 59, 59)));

        Assert.Equal(0, result.Xp);
        Assert.Equal(10, result.AllTime.TotalXp);
    }

    [Fact]
    public void Should_ReturnPositiveDelta_When_CurrentXpExceedsPreviousAtSameMoment()
    {
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 10, 13, 9)),
            Gain(Utc(2026, 10, 19, 9)), Gain(Utc(2026, 10, 20, 9)), Gain(Utc(2026, 10, 22, 9)));

        Assert.NotNull(result.Comparison);
        Assert.Equal(10, result.Comparison.PreviousXp);
        Assert.Equal(20, result.Comparison.DeltaXp);
        Assert.Equal(new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero), result.Comparison.PreviousCutoffAt);
    }

    [Fact]
    public void Should_ReturnNegativeDelta_When_CurrentXpIsBelowPreviousAtSameMoment()
    {
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 10, 12, 9)), Gain(Utc(2026, 10, 13, 9)), Gain(Utc(2026, 10, 14, 9)),
            Gain(Utc(2026, 10, 20, 9)));

        Assert.NotNull(result.Comparison);
        Assert.Equal(30, result.Comparison.PreviousXp);
        Assert.Equal(-20, result.Comparison.DeltaXp);
    }

    [Fact]
    public void Should_ReturnZeroDelta_When_XpEqualsPreviousAtSameMoment()
    {
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 10, 12, 9)), Gain(Utc(2026, 10, 13, 9)),
            Gain(Utc(2026, 10, 19, 9)), Gain(Utc(2026, 10, 21, 9)));

        Assert.NotNull(result.Comparison);
        Assert.Equal(20, result.Comparison.PreviousXp);
        Assert.Equal(0, result.Comparison.DeltaXp);
    }

    [Fact]
    public void Should_IgnorePreviousPeriodGainsAfterCutoff_When_ComparingSameMoment()
    {
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 10, 15, 13, 59, 59)), // last second before the cutoff (Thursday 14:00)
            Gain(Utc(2026, 10, 15, 14)),          // exactly the cutoff: excluded
            Gain(Utc(2026, 10, 16, 9)));          // later in the previous week: excluded

        Assert.NotNull(result.Comparison);
        Assert.Equal(10, result.Comparison.PreviousXp);
    }

    [Fact]
    public void Should_CompareToLastDayOfPreviousMonth_When_DayDoesNotExistInPreviousMonth()
    {
        // 31 October 14:00 → comparison from 1 September 00:00 to 30 September 14:00.
        var now = Utc(2026, 10, 31, 14);
        var earnings = new List<XpEarning>
        {
            Gain(Utc(2026, 9, 1)),
            Gain(Utc(2026, 9, 30, 13, 59)),
            Gain(Utc(2026, 9, 30, 14)),
        };

        var result = XpProgressCalculator.ComputeXp(earnings, CommunityPeriod.Month, now);

        Assert.NotNull(result.Comparison);
        Assert.Equal(20, result.Comparison.PreviousXp);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 14, 0, 0, TimeSpan.Zero), result.Comparison.PreviousCutoffAt);
    }

    [Fact]
    public void Should_ReturnNoComparison_When_PeriodIsGlobal()
    {
        var result = Compute(CommunityPeriod.Global, Gain(Utc(2026, 10, 12, 9)), Gain(Utc(2026, 10, 20, 9)));

        Assert.Null(result.Comparison);
        Assert.Equal(20, result.Xp);
    }

    [Fact]
    public void Should_AverageOverCalendarWeeksSinceFirstGain_When_PeriodIsGlobal()
    {
        // First gain on Sunday 2026-10-04: its week (from 09-28) counts in full →
        // 4 weeks (09-28, 10-05, 10-12, 10-19). 50 / 4 = 12.5 → 13 (away from zero).
        var result = Compute(
            CommunityPeriod.Global,
            Gain(Utc(2026, 10, 4, 23)), Gain(Utc(2026, 10, 6)), Gain(Utc(2026, 10, 13)),
            Gain(Utc(2026, 10, 14)), Gain(Utc(2026, 10, 20)));

        Assert.Equal(13, result.AverageWeeklyXp);
        Assert.Equal(new DateOnly(2026, 10, 4), result.WindowStart);
    }

    [Fact]
    public void Should_ReturnTotalAsAverage_When_FirstGainIsThisWeek()
    {
        var result = Compute(
            CommunityPeriod.Global,
            Gain(Utc(2026, 10, 20, 9)), Gain(Utc(2026, 10, 21, 9)), Gain(Utc(2026, 10, 22, 9)));

        Assert.Equal(30, result.AverageWeeklyXp);
    }

    [Fact]
    public void Should_ReturnNoAverage_When_PeriodIsNotGlobal()
    {
        var result = Compute(CommunityPeriod.Week, Gain(Utc(2026, 10, 20, 9)));

        Assert.Null(result.AverageWeeklyXp);
    }

    [Fact]
    public void Should_KeepEarliestPeriodAsRecord_When_TwoPeriodsTie()
    {
        // Week of 09-21 (September) and week of 10-05 (October): 30 XP each.
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 9, 21, 9)), Gain(Utc(2026, 9, 22, 9)), Gain(Utc(2026, 9, 23, 9)),
            Gain(Utc(2026, 10, 5, 9)), Gain(Utc(2026, 10, 6, 9)), Gain(Utc(2026, 10, 7, 9)));

        Assert.Equal(new XpRecordViewModel(new DateOnly(2026, 9, 21), 30), result.BestWeek);
        Assert.Equal(new XpRecordViewModel(new DateOnly(2026, 9, 1), 30), result.BestMonth);
    }

    [Fact]
    public void Should_ReturnNoRecordAndNoFirstGain_When_UserHasNoGain()
    {
        var result = Compute(CommunityPeriod.Week);

        Assert.Equal(0, result.Xp);
        Assert.Null(result.BestWeek);
        Assert.Null(result.BestMonth);
        Assert.Equal(new AllTimeViewModel(0, 0, null), result.AllTime);
        Assert.NotNull(result.Comparison);
        Assert.Equal(0, result.Comparison.PreviousXp);
        Assert.Equal(0, result.Comparison.DeltaXp);
    }

    [Fact]
    public void Should_ReturnEightWeeklyBarsWithZeros_When_PeriodIsWeek()
    {
        var result = Compute(CommunityPeriod.Week, Gain(Utc(2026, 10, 6, 9), 40));

        Assert.Equal(
            [
                new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 21),
                new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 19),
            ],
            result.Bars.Select(b => b.PeriodStart));
        Assert.Equal([0, 0, 0, 0, 0, 40, 0, 0], result.Bars.Select(b => b.Xp));
    }

    [Fact]
    public void Should_FlagCurrentAndRecordBars_When_RecordWeekIsDisplayed()
    {
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 10, 5, 9)), Gain(Utc(2026, 10, 6, 9)), Gain(Utc(2026, 10, 7, 9)),
            Gain(Utc(2026, 10, 20, 9)));

        var current = Assert.Single(result.Bars, b => b.IsCurrent);
        Assert.Equal(new DateOnly(2026, 10, 19), current.PeriodStart);
        Assert.Same(result.Bars[^1], current);

        var record = Assert.Single(result.Bars, b => b.IsRecord);
        Assert.Equal(new XpBarViewModel(new DateOnly(2026, 10, 5), 30, IsCurrent: false, IsRecord: true), record);
    }

    [Fact]
    public void Should_FlagNoRecordBar_When_RecordWeekIsOlderThanEightWeeks()
    {
        var result = Compute(
            CommunityPeriod.Week,
            Gain(Utc(2026, 7, 20, 9)), Gain(Utc(2026, 7, 21, 9)),
            Gain(Utc(2026, 10, 20, 9)));

        Assert.Equal(new DateOnly(2026, 7, 20), result.BestWeek?.PeriodStart);
        Assert.DoesNotContain(result.Bars, b => b.IsRecord);
    }

    [Fact]
    public void Should_ReturnOneBarPerMonthSinceFirstGain_When_PeriodIsMonth()
    {
        var result = Compute(
            CommunityPeriod.Month,
            Gain(Utc(2026, 7, 16, 9)),
            Gain(Utc(2026, 8, 10, 9), 20), Gain(Utc(2026, 8, 31, 23, 59, 59), 20),
            Gain(Utc(2026, 10, 5, 9)));

        Assert.Equal(
            [
                new XpBarViewModel(new DateOnly(2026, 7, 1), 10, IsCurrent: false, IsRecord: false),
                new XpBarViewModel(new DateOnly(2026, 8, 1), 40, IsCurrent: false, IsRecord: true),
                new XpBarViewModel(new DateOnly(2026, 9, 1), 0, IsCurrent: false, IsRecord: false),
                new XpBarViewModel(new DateOnly(2026, 10, 1), 10, IsCurrent: true, IsRecord: false),
            ],
            result.Bars);
        Assert.Equal(new DateOnly(2026, 10, 1), result.WindowStart);
        Assert.Equal(10, result.Xp);
    }

    [Fact]
    public void Should_ReturnOnlyCurrentMonthBar_When_UserHasNoGainAndPeriodIsMonth()
    {
        var result = Compute(CommunityPeriod.Month);

        var bar = Assert.Single(result.Bars);
        Assert.Equal(new XpBarViewModel(new DateOnly(2026, 10, 1), 0, IsCurrent: true, IsRecord: false), bar);
    }

    [Fact]
    public void Should_ReturnCumulativeMonthEndBarsThenToday_When_PeriodIsGlobal()
    {
        var result = Compute(
            CommunityPeriod.Global,
            Gain(Utc(2026, 7, 16, 9)),
            Gain(Utc(2026, 8, 10, 9), 20),
            Gain(Utc(2026, 10, 5, 9), 30));

        Assert.Equal(
            [
                new XpBarViewModel(new DateOnly(2026, 7, 1), 10, IsCurrent: false, IsRecord: false),
                new XpBarViewModel(new DateOnly(2026, 8, 1), 30, IsCurrent: false, IsRecord: false),
                new XpBarViewModel(new DateOnly(2026, 9, 1), 30, IsCurrent: false, IsRecord: false),
                new XpBarViewModel(new DateOnly(2026, 10, 1), 60, IsCurrent: true, IsRecord: false),
            ],
            result.Bars);
        Assert.Equal(60, result.Xp);
        Assert.Equal(new DateOnly(2026, 7, 16), result.WindowStart);
    }

    [Fact]
    public void Should_ReturnNoBar_When_UserHasNoGainAndPeriodIsGlobal()
    {
        var result = Compute(CommunityPeriod.Global);

        Assert.Empty(result.Bars);
        Assert.Null(result.WindowStart);
        Assert.Null(result.AverageWeeklyXp);
    }

    [Fact]
    public void Should_ExposeUtcInstants_When_EarningsKindIsUnspecified()
    {
        // EF Core reads datetime2 columns as DateTimeKind.Unspecified.
        var earnedAt = new DateTime(2026, 7, 16, 9, 12, 0, DateTimeKind.Unspecified);

        var result = Compute(CommunityPeriod.Global, Gain(earnedAt));

        Assert.NotNull(result.AllTime.FirstGainAt);
        Assert.Equal(TimeSpan.Zero, result.AllTime.FirstGainAt.Value.Offset);
        Assert.Equal(new DateTimeOffset(2026, 7, 16, 9, 12, 0, TimeSpan.Zero), result.AllTime.FirstGainAt.Value);
    }
}
