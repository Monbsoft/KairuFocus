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
    Task<PomodoroSession?> GetLatestCompletedTodayAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PomodoroSession>> GetTodaySprintSessionsAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count of completed sprint sessions whose EndedAt falls within [startUtc, endUtc).
    /// Uses UTC range comparison (provider-safe: no .Date or AddMinutes in SQL).
    /// </summary>
    Task<int> GetCompletedSprintsTodayCountAsync(UserId userId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// All sprint sessions completed within [startUtc, endUtc).
    /// Covers both free and regular sprints. Used to sum today's focus time.
    /// Uses UTC range comparison (provider-safe).
    /// </summary>
    Task<IReadOnlyList<PomodoroSession>> GetCompletedSprintSessionsTodayAsync(UserId userId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// EndedAt (UTC) of all completed sprint sessions for the user, unfiltered by date.
    /// The Application layer maps these to local dates using the user's UTC offset.
    /// No date bucketing in SQL — safe for both SQL Server and SQLite providers.
    /// </summary>
    Task<IReadOnlyList<DateTime>> GetCompletedSprintEndTimesAsync(UserId userId, CancellationToken cancellationToken = default);
}
