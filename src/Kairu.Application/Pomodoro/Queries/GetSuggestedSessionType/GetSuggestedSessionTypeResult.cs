using Kairu.Domain.Pomodoro;

namespace Kairu.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed record GetSuggestedSessionTypeResult(
    PomodoroSessionType SuggestedType,
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);
