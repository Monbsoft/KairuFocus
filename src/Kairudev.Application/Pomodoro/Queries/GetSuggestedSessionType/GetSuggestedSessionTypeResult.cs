namespace Kairudev.Application.Pomodoro.Queries.GetSuggestedSessionType;

public sealed record GetSuggestedSessionTypeResult(
    string SuggestedType,
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);
