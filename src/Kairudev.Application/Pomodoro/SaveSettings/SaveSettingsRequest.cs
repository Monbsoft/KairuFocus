namespace Kairudev.Application.Pomodoro.SaveSettings;

public sealed record SaveSettingsRequest(
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);
