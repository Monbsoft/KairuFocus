using Kairu.Domain.Pomodoro;

namespace Kairu.Domain.Tests.Pomodoro;

public sealed class PomodoroSettingsTests
{
    [Fact]
    public void Should_CreateWithValidDurations()
    {
        var result = PomodoroSettings.Create(25, 5, 15);

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
        var result = PomodoroSettings.Create(sprint, shortBreak, longBreak);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_ReturnFailure_When_SprintDurationTooLong()
    {
        var result = PomodoroSettings.Create(PomodoroSettings.MaxDurationMinutes + 1, 5, 15);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(25, 0,  15)]
    [InlineData(25, -1, 15)]
    public void Should_ReturnFailure_When_ShortBreakTooShort(int sprint, int shortBreak, int longBreak)
    {
        var result = PomodoroSettings.Create(sprint, shortBreak, longBreak);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(25, 5, 0)]
    [InlineData(25, 5, -1)]
    public void Should_ReturnFailure_When_LongBreakTooShort(int sprint, int shortBreak, int longBreak)
    {
        var result = PomodoroSettings.Create(sprint, shortBreak, longBreak);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_HaveFixedSprintsBeforeLongBreak()
    {
        Assert.Equal(4, PomodoroSettings.SprintsBeforeLongBreak);
    }
}
