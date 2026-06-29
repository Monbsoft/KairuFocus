namespace KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

/// <summary>
/// Pure streak computation (no I/O). The streak is the number of consecutive days,
/// including today, on which the user completed at least one sprint; it breaks at the
/// first day without a completed sprint. This counting rule lives in the Application
/// layer — the repository only supplies the distinct UTC-day dates of completed sprints.
/// </summary>
internal static class StreakCalculator
{
    /// <summary>
    /// Number of consecutive days (including 'today') with >= 1 completed sprint.
    /// The streak breaks at the first day without a completed sprint.
    /// Robust to duplicate and unordered input dates.
    /// </summary>
    public static int Compute(IReadOnlyList<DateOnly> completedDates, DateOnly today)
    {
        var days = completedDates.ToHashSet();
        var streak = 0;
        var cursor = today;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
