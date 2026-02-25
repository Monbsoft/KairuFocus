using Kairudev.Domain.Common;

namespace Kairudev.Domain.Pomodoro;

public sealed record PomodoroSettings
{
    public const int SprintsBeforeLongBreak = 4;
    public const int MinDurationMinutes = 1;
    public const int MaxDurationMinutes = 120;

    public int SprintDurationMinutes { get; }
    public int ShortBreakDurationMinutes { get; }
    public int LongBreakDurationMinutes { get; }

    private PomodoroSettings(int sprint, int shortBreak, int longBreak)
    {
        SprintDurationMinutes = sprint;
        ShortBreakDurationMinutes = shortBreak;
        LongBreakDurationMinutes = longBreak;
    }

    public static readonly PomodoroSettings Default = new(25, 5, 15);

    public static Result<PomodoroSettings> Create(int sprint, int shortBreak, int longBreak)
    {
        if (sprint < MinDurationMinutes || sprint > MaxDurationMinutes)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDuration);

        if (shortBreak < MinDurationMinutes || shortBreak > MaxDurationMinutes)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDuration);

        if (longBreak < MinDurationMinutes || longBreak > MaxDurationMinutes)
            return Result.Failure<PomodoroSettings>(DomainErrors.Pomodoro.InvalidDuration);

        return Result.Success(new PomodoroSettings(sprint, shortBreak, longBreak));
    }
}
