using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed record GetSuggestedSessionTypeResult(
    PomodoroSessionType SuggestedType,
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);
