using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Common;

public sealed record PomodoroSettingsViewModel(
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes,
    int SprintsBeforeLongBreak)
{
    public static PomodoroSettingsViewModel From(PomodoroSettings settings) =>
        new(
            settings.SprintDurationMinutes,
            settings.ShortBreakDurationMinutes,
            settings.LongBreakDurationMinutes,
            PomodoroSettings.SprintsBeforeLongBreak);
}
