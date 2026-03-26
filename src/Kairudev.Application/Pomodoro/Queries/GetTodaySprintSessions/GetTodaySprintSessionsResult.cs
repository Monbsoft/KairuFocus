using Kairudev.Application.Pomodoro.Common;

namespace Kairudev.Application.Pomodoro.Queries.GetTodaySprintSessions;

public sealed record GetTodaySprintSessionsResult(IReadOnlyList<PomodoroSessionViewModel> Sessions);
