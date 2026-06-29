namespace KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

public sealed record GetFocusSummaryResult(
    int SprintsToday,
    int FocusMinutesToday,
    int DailySprintGoal,
    int Streak);
