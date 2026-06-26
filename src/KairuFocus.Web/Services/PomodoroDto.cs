namespace KairuFocus.Web.Services;

public sealed record PomodoroSettingsDto(
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes,
    int SprintsBeforeLongBreak,
    int DailySprintGoal);

public sealed record FocusSummaryDto(
    int SprintsToday,
    int FocusMinutesToday,
    int DailySprintGoal,
    int Streak);

public sealed record PomodoroSessionDto(
    Guid Id,
    string SessionType,
    string Status,
    int PlannedDurationMinutes,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<Guid> LinkedTaskIds,
    double? DurationSeconds);

public sealed record SuggestedSessionTypeDto(
    string SuggestedType,
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);
