using KairuFocus.Application.Gamification.Common;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class CommunityPeriodsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Should_DefaultToWeek_When_PeriodIsMissing(string? value)
    {
        var parsed = CommunityPeriods.TryParse(value, out var period);

        Assert.True(parsed);
        Assert.Equal(CommunityPeriod.Week, period);
    }

    [Theory]
    [InlineData("month", CommunityPeriod.Month)]
    [InlineData("MONTH", CommunityPeriod.Month)]
    [InlineData("Month", CommunityPeriod.Month)]
    [InlineData("week", CommunityPeriod.Week)]
    [InlineData("global", CommunityPeriod.Global)]
    public void Should_ParseMonth_When_PeriodHasDifferentCase(string value, CommunityPeriod expected)
    {
        var parsed = CommunityPeriods.TryParse(value, out var period);

        Assert.True(parsed);
        Assert.Equal(expected, period);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("7")]
    [InlineData("-1")]
    public void Should_Fail_When_PeriodIsNumeric(string value)
    {
        Assert.False(CommunityPeriods.TryParse(value, out _));
    }

    [Theory]
    [InlineData("year")]
    [InlineData("semaine")]
    [InlineData(" week ")]
    [InlineData("Week,Month")]
    [InlineData("   ")]
    public void Should_Fail_When_PeriodIsUnknown(string value)
    {
        Assert.False(CommunityPeriods.TryParse(value, out _));
    }
}
