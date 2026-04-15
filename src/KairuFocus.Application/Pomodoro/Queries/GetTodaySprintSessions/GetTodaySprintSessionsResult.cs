using KairuFocus.Application.Pomodoro.Common;

namespace KairuFocus.Application.Pomodoro.Queries.GetTodaySprintSessions;

public sealed record GetTodaySprintSessionsResult(IReadOnlyList<PomodoroSessionViewModel> Sessions);
