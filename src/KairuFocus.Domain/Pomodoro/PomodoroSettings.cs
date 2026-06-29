using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Pomodoro;

public sealed record PomodoroSettings
{
    public const int SprintsBeforeLongBreak = 4;
    public const int MinDurationMinutes = 1;
    public const int MaxDurationMinutes = 120;

    public const int MinDailySprintGoal = 1;
    public const int MaxDailySprintGoal = 16;
    public const int DefaultDailySprintGoal = 4;

    public int SprintDurationMinutes { get; }
    public int ShortBreakDurationMinutes { get; }
    public int LongBreakDurationMinutes { get; }
    public int DailySprintGoal { get; }

    private PomodoroSettings(int sprint, int shortBreak, int longBreak, int dailySprintGoal)
    {
        SprintDurationMinutes = sprint;
        ShortBreakDurationMinutes = shortBreak;
        LongBreakDurationMinutes = longBreak;
        DailySprintGoal = dailySprintGoal;
    }

    public static readonly PomodoroSettings Default = new(25, 5, 15, DefaultDailySprintGoal);

    public static Result<PomodoroSettings> Create(int sprint, int shortBreak, int longBreak, int dailySprintGoal)
    {
        if (sprint < MinDurationMinutes || sprint > MaxDurationMinutes)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDuration);

        if (shortBreak < MinDurationMinutes || shortBreak > MaxDurationMinutes)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDuration);

        if (longBreak < MinDurationMinutes || longBreak > MaxDurationMinutes)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDuration);

        if (dailySprintGoal < MinDailySprintGoal || dailySprintGoal > MaxDailySprintGoal)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDailySprintGoal);

        return Result.Success(new PomodoroSettings(sprint, shortBreak, longBreak, dailySprintGoal));
    }
}
