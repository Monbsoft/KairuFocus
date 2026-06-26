using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Domain.Tests.Pomodoro;

public sealed class PomodoroSettingsTests
{
    private const int ValidGoal = 4;

    [Fact]
    public void Should_CreateWithValidDurations()
    {
        var result = PomodoroSettings.Create(25, 5, 15, ValidGoal);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value.SprintDurationMinutes);
        Assert.Equal(5,  result.Value.ShortBreakDurationMinutes);
        Assert.Equal(15, result.Value.LongBreakDurationMinutes);
    }

    [Fact]
    public void Should_HaveDefaultValues()
    {
        var defaults = PomodoroSettings.Default;

        Assert.Equal(25, defaults.SprintDurationMinutes);
        Assert.Equal(5,  defaults.ShortBreakDurationMinutes);
        Assert.Equal(15, defaults.LongBreakDurationMinutes);
    }

    [Theory]
    [InlineData(0,  5, 15)]
    [InlineData(-1, 5, 15)]
    public void Should_ReturnFailure_When_SprintDurationTooShort(int sprint, int shortBreak, int longBreak)
    {
        var result = PomodoroSettings.Create(sprint, shortBreak, longBreak, ValidGoal);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_ReturnFailure_When_SprintDurationTooLong()
    {
        var result = PomodoroSettings.Create(PomodoroSettings.MaxDurationMinutes + 1, 5, 15, ValidGoal);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(25, 0,  15)]
    [InlineData(25, -1, 15)]
    public void Should_ReturnFailure_When_ShortBreakTooShort(int sprint, int shortBreak, int longBreak)
    {
        var result = PomodoroSettings.Create(sprint, shortBreak, longBreak, ValidGoal);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(25, 5, 0)]
    [InlineData(25, 5, -1)]
    public void Should_ReturnFailure_When_LongBreakTooShort(int sprint, int shortBreak, int longBreak)
    {
        var result = PomodoroSettings.Create(sprint, shortBreak, longBreak, ValidGoal);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_HaveFixedSprintsBeforeLongBreak()
    {
        Assert.Equal(4, PomodoroSettings.SprintsBeforeLongBreak);
    }

    // ── Daily sprint goal ──────────────────────────────────────────────────

    [Fact]
    public void Should_UseFour_When_DefaultSettings()
    {
        Assert.Equal(4, PomodoroSettings.Default.DailySprintGoal);
        Assert.Equal(4, PomodoroSettings.DefaultDailySprintGoal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    public void Should_CreateSettings_When_DailySprintGoalWithinBounds(int goal)
    {
        var result = PomodoroSettings.Create(25, 5, 15, goal);

        Assert.True(result.IsSuccess);
        Assert.Equal(goal, result.Value.DailySprintGoal);
    }

    [Fact]
    public void Should_Fail_When_DailySprintGoalBelowMin()
    {
        var result = PomodoroSettings.Create(25, 5, 15, 0);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Pomodoro.InvalidDailySprintGoal, result.Error);
    }

    [Fact]
    public void Should_Fail_When_DailySprintGoalAboveMax()
    {
        var result = PomodoroSettings.Create(25, 5, 15, 17);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Pomodoro.InvalidDailySprintGoal, result.Error);
    }

    [Fact]
    public void Should_StillValidateDurations_When_DailySprintGoalValid()
    {
        var result = PomodoroSettings.Create(0, 5, 15, ValidGoal);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Pomodoro.InvalidDuration, result.Error);
    }
}
