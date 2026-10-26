using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class SprintActivityCalculatorTests
{
    // Week [Mon 2026-10-19, Mon 2026-10-26)
    private static readonly UtcRange Week = new(
        new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc));

    private static CompletedSprintInterval Sprint(DateTime startedAt, TimeSpan duration) =>
        new(startedAt, startedAt + duration);

    private static DateTime Tuesday(int hour) => new(2026, 10, 20, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_CountAllCompletedSprints_When_SomeLastUnder5Minutes()
    {
        var intervals = new[]
        {
            Sprint(Tuesday(9), TimeSpan.FromMinutes(3)),
            Sprint(Tuesday(10), TimeSpan.FromMinutes(25)),
        };

        var (sprints, focus) = SprintActivityCalculator.Summarize(intervals, Week);

        Assert.Equal(2, sprints);
        Assert.Equal(TimeSpan.FromMinutes(28), focus);
    }

    [Fact]
    public void Should_CapEachSprintAt4Hours_When_SummingFocus()
    {
        var intervals = new[]
        {
            Sprint(Tuesday(1), TimeSpan.FromHours(6)),
            Sprint(Tuesday(12), TimeSpan.FromHours(1)),
        };

        var (sprints, focus) = SprintActivityCalculator.Summarize(intervals, Week);

        Assert.Equal(2, sprints);
        Assert.Equal(TimeSpan.FromHours(5), focus);
    }

    [Fact]
    public void Should_IgnoreSprintsEndingOutsideWindow_When_Summarizing()
    {
        var intervals = new[]
        {
            Sprint(Week.StartUtc.AddMinutes(-25), TimeSpan.FromMinutes(25)), // ends exactly at the start: counted, in full
            Sprint(Week.StartUtc.AddMinutes(-30), TimeSpan.FromMinutes(25)), // ends before the start
            Sprint(Week.EndUtc.AddMinutes(-25), TimeSpan.FromMinutes(25)),   // ends exactly at the end: excluded
        };

        var (sprints, focus) = SprintActivityCalculator.Summarize(intervals, Week);

        Assert.Equal(1, sprints);
        Assert.Equal(TimeSpan.FromMinutes(25), focus);
    }

    [Fact]
    public void Should_RoundOnce_When_ConvertingFocusToMinutes()
    {
        // 3 × 20 min 20 s = 61 min. Rounding each sprint first would give 3 × 20 = 60.
        var intervals = Enumerable.Range(0, 3)
            .Select(i => Sprint(Tuesday(9 + i), TimeSpan.FromSeconds(20 * 60 + 20)))
            .ToList();

        var (_, focus) = SprintActivityCalculator.Summarize(intervals, Week);

        Assert.Equal(61, SprintActivityCalculator.ToRoundedMinutes(focus));
    }
}
