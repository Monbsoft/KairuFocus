using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Pomodoro;

public interface IPomodoroSessionRepository
{
    Task AddAsync(PomodoroSession session, CancellationToken cancellationToken = default);
    Task<PomodoroSession?> GetByIdAsync(PomodoroSessionId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PomodoroSession>> GetByIdsAsync(IEnumerable<PomodoroSessionId> ids, CancellationToken cancellationToken = default);
    Task<PomodoroSession?> GetActiveAsync(UserId userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(PomodoroSession session, CancellationToken cancellationToken = default);
    Task<int> GetCompletedTodayCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<int> GetCompletedSprintsTodayCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<PomodoroSession?> GetLatestCompletedTodayAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PomodoroSession>> GetTodaySprintSessionsAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// All sprint sessions completed today (UTC day). Criterion aligned with
    /// GetCompletedSprintsTodayCountAsync (Sprint + Completed + EndedAt UTC day),
    /// covering both free and regular sprints. Used to sum today's focus time.
    /// </summary>
    Task<IReadOnlyList<PomodoroSession>> GetCompletedSprintSessionsTodayAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Distinct UTC-day dates for which the user completed >= 1 sprint, ordered descending.
    /// Criterion aligned with GetCompletedSprintsTodayCountAsync (Sprint + Completed + EndedAt UTC day).
    /// </summary>
    Task<IReadOnlyList<DateOnly>> GetCompletedSprintDatesAsync(UserId userId, CancellationToken cancellationToken = default);
}
