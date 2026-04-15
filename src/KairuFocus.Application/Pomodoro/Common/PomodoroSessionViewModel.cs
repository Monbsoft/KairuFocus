using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Pomodoro.Common;

public sealed record PomodoroSessionViewModel(
    Guid Id,
    string SessionType,
    string Status,
    int PlannedDurationMinutes,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<Guid> LinkedTaskIds,
    double? DurationSeconds)
{
    public static PomodoroSessionViewModel From(PomodoroSession session)
    {
        double? duration = session.StartedAt.HasValue && session.EndedAt.HasValue
            ? (session.EndedAt.Value - session.StartedAt.Value).TotalSeconds
            : null;

        return new(
            session.Id.Value,
            session.SessionType.ToString(),
            session.Status.ToString(),
            session.PlannedDurationMinutes,
            session.StartedAt,
            session.EndedAt,
            session.LinkedTaskIds.Select(t => t.Value).ToList().AsReadOnly(),
            duration);
    }
}
