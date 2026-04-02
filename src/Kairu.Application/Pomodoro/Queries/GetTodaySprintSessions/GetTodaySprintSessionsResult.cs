using Kairu.Application.Pomodoro.Common;

namespace Kairu.Application.Pomodoro.Queries.GetTodaySprintSessions;

public sealed record GetTodaySprintSessionsResult(IReadOnlyList<PomodoroSessionViewModel> Sessions);
