using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Common;

public sealed record PomodoroSessionViewModel(
    Guid Id,
    string SessionType,
    string Status,
    int PlannedDurationMinutes,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<Guid> LinkedTaskIds)
{
    public static PomodoroSessionViewModel From(PomodoroSession session) =>
        new(
            session.Id.Value,
            session.SessionType.ToString(),
            session.Status.ToString(),
            session.PlannedDurationMinutes,
            session.StartedAt,
            session.EndedAt,
            session.LinkedTaskIds.Select(t => t.Value).ToList().AsReadOnly());
}
