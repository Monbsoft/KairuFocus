using KairuFocus.Application.Gamification.Common;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class CommunityCalendarTests
{
    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    [Fact]
    public void Should_StartOnFirstDayOfMonth_When_ComputingMonthWindow()
    {
        var window = CommunityCalendar.CurrentWindow(CommunityPeriod.Month, Utc(2026, 10, 22, 14), firstGainUtc: null);

        Assert.Equal(new UtcRange(Utc(2026, 10, 1), Utc(2026, 11, 1)), window);
    }

    [Fact]
    public void Should_EndOnFirstDayOfNextYear_When_MonthIsDecember()
    {
        var window = CommunityCalendar.CurrentWindow(CommunityPeriod.Month, Utc(2026, 12, 15, 9), firstGainUtc: null);

        Assert.Equal(new UtcRange(Utc(2026, 12, 1), Utc(2027, 1, 1)), window);
    }

    [Fact]
    public void Should_StartOnMonday_When_ComputingWeekWindow()
    {
        // Thursday 2026-10-22 → [Monday 2026-10-19, Monday 2026-10-26)
        var window = CommunityCalendar.CurrentWindow(CommunityPeriod.Week, Utc(2026, 10, 22, 14), firstGainUtc: null);

        Assert.Equal(new UtcRange(Utc(2026, 10, 19), Utc(2026, 10, 26)), window);
    }

    [Fact]
    public void Should_CompareToSameDayAndHourLastWeek_When_PeriodIsWeek()
    {
        // Thursday 14:00 → previous week from Monday 00:00 to Thursday 14:00.
        var previous = CommunityCalendar.PreviousSameMoment(CommunityPeriod.Week, Utc(2026, 10, 22, 14));

        Assert.Equal(new UtcRange(Utc(2026, 10, 12), Utc(2026, 10, 15, 14)), previous);
    }

    [Fact]
    public void Should_CompareToSameDateAndHourLastMonth_When_PeriodIsMonth()
    {
        var previous = CommunityCalendar.PreviousSameMoment(CommunityPeriod.Month, Utc(2026, 10, 22, 14, 30));

        Assert.Equal(new UtcRange(Utc(2026, 9, 1), Utc(2026, 9, 22, 14, 30)), previous);
    }

    [Theory]
    [InlineData(2026, 10, 31, 2026, 9, 30)]  // September has 30 days
    [InlineData(2026, 3, 31, 2026, 2, 28)]   // common year
    [InlineData(2026, 3, 29, 2026, 2, 28)]   // common year, 29 does not exist either
    [InlineData(2028, 3, 31, 2028, 2, 29)]   // leap year
    public void Should_CompareToLastDayOfPreviousMonth_When_DayDoesNotExistInPreviousMonth(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        var previous = CommunityCalendar.PreviousSameMoment(CommunityPeriod.Month, Utc(year, month, day, 14));

        Assert.Equal(Utc(expectedYear, expectedMonth, 1), previous.StartUtc);
        Assert.Equal(Utc(expectedYear, expectedMonth, expectedDay, 14), previous.EndUtc);
    }

    [Fact]
    public void Should_CompareToDecember_When_MonthIsJanuary()
    {
        var previous = CommunityCalendar.PreviousSameMoment(CommunityPeriod.Month, Utc(2027, 1, 15, 10));

        Assert.Equal(new UtcRange(Utc(2026, 12, 1), Utc(2026, 12, 15, 10)), previous);
    }

    [Fact]
    public void Should_Throw_When_PreviousPeriodIsRequestedForGlobal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CommunityCalendar.PreviousSameMoment(CommunityPeriod.Global, Utc(2026, 10, 22)));
    }

    [Fact]
    public void Should_ReturnNoWindow_When_PeriodIsGlobalAndUserHasNoGain()
    {
        var window = CommunityCalendar.CurrentWindow(CommunityPeriod.Global, Utc(2026, 10, 22), firstGainUtc: null);

        Assert.Null(window);
    }

    [Fact]
    public void Should_StartAtFirstGain_When_PeriodIsGlobal()
    {
        var firstGain = Utc(2026, 7, 16, 9, 12);

        var window = CommunityCalendar.CurrentWindow(CommunityPeriod.Global, Utc(2026, 10, 22), firstGain);

        Assert.Equal(new UtcRange(firstGain, DateTime.MaxValue), window);
    }

    [Theory]
    [InlineData(2026, 10, 22, 1)]  // same week (Thursday)
    [InlineData(2026, 10, 19, 1)]  // same week (Monday)
    [InlineData(2026, 10, 18, 2)]  // Sunday of the previous week
    [InlineData(2026, 10, 4, 4)]   // weeks of 09-28, 10-05, 10-12, 10-19
    public void Should_CountCurrentWeek_When_ComputingWeeksSpanned(int year, int month, int day, int expectedWeeks)
    {
        var weeks = CommunityCalendar.WeeksSpanned(Utc(year, month, day, 23), Utc(2026, 10, 22, 14));

        Assert.Equal(expectedWeeks, weeks);
    }
}
