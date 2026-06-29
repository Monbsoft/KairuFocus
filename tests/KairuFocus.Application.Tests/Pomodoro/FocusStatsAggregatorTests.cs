using KairuFocus.Application.Pomodoro.Queries.GetFocusStats;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class FocusStatsAggregatorTests
{
    private static readonly TimeSpan Utc = TimeSpan.Zero;
    private static readonly TimeSpan Plus2 = TimeSpan.FromHours(2);

    private static CompletedSprintInterval Sprint(DateTime startedUtc, int minutes) =>
        new(startedUtc, startedUtc.AddMinutes(minutes));

    [Fact]
    public void Should_ReturnEmpty_When_NoIntervals()
    {
        var days = FocusStatsAggregator.Aggregate([], Utc);

        Assert.Empty(days);
    }

    [Fact]
    public void Should_CountSprintsAndSumMinutes_When_SingleDay()
    {
        var d = new DateTime(2026, 6, 26, 9, 0, 0, DateTimeKind.Utc);
        var intervals = new List<CompletedSprintInterval>
        {
            Sprint(d, 25),
            Sprint(d.AddHours(2), 10),
        };

        var days = FocusStatsAggregator.Aggregate(intervals, Utc);

        var day = Assert.Single(days);
        Assert.Equal(new DateOnly(2026, 6, 26), day.Date);
        Assert.Equal(2, day.SprintCount);
        Assert.Equal(35, day.FocusMinutes);
    }

    [Fact]
    public void Should_BucketByLocalDateOfEndedAt_When_OffsetPositive()
    {
        // Ends 2026-06-26 23:30 UTC. With +2h => local 2026-06-27 01:30 => next local day.
        var started = new DateTime(2026, 6, 26, 23, 5, 0, DateTimeKind.Utc);
        var intervals = new List<CompletedSprintInterval> { Sprint(started, 25) };

        var days = FocusStatsAggregator.Aggregate(intervals, Plus2);

        var day = Assert.Single(days);
        Assert.Equal(new DateOnly(2026, 6, 27), day.Date);
    }

    [Fact]
    public void Should_ProduceOneEntryPerDay_SortedAscending_When_MultipleDays()
    {
        var d = new DateTime(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc);
        var intervals = new List<CompletedSprintInterval>
        {
            Sprint(d.AddDays(2), 25), // 2026-06-26
            Sprint(d, 25),            // 2026-06-24
            Sprint(d.AddDays(1), 25), // 2026-06-25
            Sprint(d.AddDays(1).AddHours(1), 25), // 2026-06-25 again
        };

        var days = FocusStatsAggregator.Aggregate(intervals, Utc);

        Assert.Equal(3, days.Count);
        Assert.Equal(new DateOnly(2026, 6, 24), days[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 25), days[1].Date);
        Assert.Equal(2, days[1].SprintCount);
        Assert.Equal(new DateOnly(2026, 6, 26), days[2].Date);
    }

    [Fact]
    public void Should_RoundFocusMinutes_When_DurationHasSeconds()
    {
        var start = new DateTime(2026, 6, 26, 9, 0, 0, DateTimeKind.Utc);
        // 90 secondes => 1.5 min => arrondi à 2.
        var intervals = new List<CompletedSprintInterval>
        {
            new(start, start.AddSeconds(90)),
        };

        var days = FocusStatsAggregator.Aggregate(intervals, Utc);

        Assert.Equal(2, Assert.Single(days).FocusMinutes);
    }
}
