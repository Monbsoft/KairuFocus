using KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 6, 26);

    [Fact]
    public void Should_ReturnZero_When_NoCompletedSprintDates()
    {
        var streak = StreakCalculator.Compute([], Today);

        Assert.Equal(0, streak);
    }

    [Fact]
    public void Should_ReturnZero_When_TodayHasNoSprintButYesterdayDoes()
    {
        var dates = new List<DateOnly> { Today.AddDays(-1) };

        var streak = StreakCalculator.Compute(dates, Today);

        Assert.Equal(0, streak);
    }

    [Fact]
    public void Should_ReturnOne_When_OnlyTodayHasSprint()
    {
        var dates = new List<DateOnly> { Today };

        var streak = StreakCalculator.Compute(dates, Today);

        Assert.Equal(1, streak);
    }

    [Fact]
    public void Should_ReturnThree_When_ThreeConsecutiveDaysIncludingToday()
    {
        var dates = new List<DateOnly> { Today, Today.AddDays(-1), Today.AddDays(-2) };

        var streak = StreakCalculator.Compute(dates, Today);

        Assert.Equal(3, streak);
    }

    [Fact]
    public void Should_StopAtGap_When_DaysAreNotConsecutive()
    {
        // today, today-1, [gap at today-2], today-3 → streak of 2
        var dates = new List<DateOnly> { Today, Today.AddDays(-1), Today.AddDays(-3) };

        var streak = StreakCalculator.Compute(dates, Today);

        Assert.Equal(2, streak);
    }

    [Fact]
    public void Should_BeRobustToDuplicates_When_SameDayAppearsTwice()
    {
        var dates = new List<DateOnly> { Today, Today, Today.AddDays(-1), Today.AddDays(-1) };

        var streak = StreakCalculator.Compute(dates, Today);

        Assert.Equal(2, streak);
    }

    [Fact]
    public void Should_BeRobustToUnsortedInput_When_DatesNotOrdered()
    {
        var dates = new List<DateOnly> { Today.AddDays(-2), Today, Today.AddDays(-1) };

        var streak = StreakCalculator.Compute(dates, Today);

        Assert.Equal(3, streak);
    }
}
