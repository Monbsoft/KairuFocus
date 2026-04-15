using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed record GetSuggestedSessionTypeResult(
    PomodoroSessionType SuggestedType,
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);
