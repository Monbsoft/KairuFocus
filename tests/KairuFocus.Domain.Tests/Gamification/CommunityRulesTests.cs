using KairuFocus.Domain.Gamification;

namespace KairuFocus.Domain.Tests.Gamification;

public sealed class CommunityRulesTests
{
    private const int FreeSprint = 0;
    private static readonly DateTime Start = new(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc);

    // ── CountedFocus ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(3)]
    [InlineData(25)]
    [InlineData(239)]
    public void Should_ReturnActualDuration_When_SprintLastsLessThan4Hours(int minutes)
    {
        var focus = CommunityRules.CountedFocus(Start, Start.AddMinutes(minutes));

        Assert.Equal(TimeSpan.FromMinutes(minutes), focus);
    }

    [Theory]
    [InlineData(240)]
    [InlineData(241)]
    [InlineData(360)]
    [InlineData(1800)]
    public void Should_Return4Hours_When_SprintLastsLonger(int minutes)
    {
        var focus = CommunityRules.CountedFocus(Start, Start.AddMinutes(minutes));

        Assert.Equal(TimeSpan.FromHours(4), focus);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-90)]
    public void Should_ReturnZero_When_EndPrecedesStart(int minutes)
    {
        var focus = CommunityRules.CountedFocus(Start, Start.AddMinutes(minutes));

        Assert.Equal(TimeSpan.Zero, focus);
    }

    // ── IsSprintInProgress ───────────────────────────────────────────────────

    [Fact]
    public void Should_BeInProgress_When_TimedSprintIsBeforeItsDeadline()
    {
        var now = Start.AddMinutes(24).AddSeconds(59);

        Assert.True(CommunityRules.IsSprintInProgress(Start, 25, now));
    }

    [Fact]
    public void Should_NotBeInProgress_When_TimedSprintDeadlineHasPassed()
    {
        // 25-minute sprint started 30 minutes ago and still Active (never observed).
        var now = Start.AddMinutes(30);

        Assert.False(CommunityRules.IsSprintInProgress(Start, 25, now));
    }

    [Fact]
    public void Should_NotBeInProgress_When_NowEqualsDeadline()
    {
        var now = Start.AddMinutes(25);

        Assert.False(CommunityRules.IsSprintInProgress(Start, 25, now));
    }

    [Fact]
    public void Should_BeInProgress_When_FreeSprintStartedLessThan4HoursAgo()
    {
        var now = Start.AddHours(3).AddMinutes(59);

        Assert.True(CommunityRules.IsSprintInProgress(Start, FreeSprint, now));
    }

    [Fact]
    public void Should_NotBeInProgress_When_FreeSprintStarted4HoursAgo()
    {
        var now = Start.AddHours(4);

        Assert.False(CommunityRules.IsSprintInProgress(Start, FreeSprint, now));
    }

    [Fact]
    public void Should_BeInProgress_When_StartedAtIsSlightlyAfterNow()
    {
        // Clock skew between instances: no lower bound.
        var now = Start.AddSeconds(-2);

        Assert.True(CommunityRules.IsSprintInProgress(Start, 25, now));
    }

    [Fact]
    public void Should_NotBeInProgress_When_PlannedDurationExceeds4HoursAnd4HoursHavePassed()
    {
        var now = Start.AddHours(4);

        Assert.False(CommunityRules.IsSprintInProgress(Start, 300, now));
    }
}
