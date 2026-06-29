namespace KairuFocus.Application.Pomodoro.Queries.GetFocusStats;

/// <summary>
/// Activité de focus agrégée pour une journée locale.
/// </summary>
public sealed record FocusDayStat(DateOnly Date, int SprintCount, int FocusMinutes);
