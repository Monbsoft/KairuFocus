using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KairuFocus.Infrastructure.Persistence;

internal sealed class EfCorePomodoroSettingsRepository : IPomodoroSettingsRepository
{
    private readonly KairuFocusDbContext _context;
    private readonly ILogger<EfCorePomodoroSettingsRepository> _logger;

    public EfCorePomodoroSettingsRepository(
        KairuFocusDbContext context,
        ILogger<EfCorePomodoroSettingsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PomodoroSettings> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.PomodoroSettings
            .FirstOrDefaultAsync(s => s.UserId == userId.Value.ToString(), cancellationToken);

        if (row is null)
            return PomodoroSettings.Default;

        var result = PomodoroSettings.Create(
            row.SprintDurationMinutes,
            row.ShortBreakDurationMinutes,
            row.LongBreakDurationMinutes,
            row.DailySprintGoal);

        if (result.IsFailure)
        {
            // A persisted row failed domain validation (manual edit, migration drift,
            // or a value written before a constraint was tightened). Surface it instead
            // of silently masking the user's real configuration with defaults.
            _logger.LogWarning(
                "Persisted PomodoroSettings for user {UserId} failed domain validation " +
                "(sprint={Sprint}, shortBreak={ShortBreak}, longBreak={LongBreak}, dailyGoal={DailyGoal}); " +
                "falling back to defaults.",
                userId.Value,
                row.SprintDurationMinutes,
                row.ShortBreakDurationMinutes,
                row.LongBreakDurationMinutes,
                row.DailySprintGoal);

            return PomodoroSettings.Default;
        }

        return result.Value;
    }

    public async Task SaveAsync(PomodoroSettings settings, UserId userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.PomodoroSettings
            .FirstOrDefaultAsync(s => s.UserId == userId.Value.ToString(), cancellationToken);

        if (row is null)
        {
            row = new PomodoroSettingsRow
            {
                UserId = userId.Value.ToString(),
                SprintDurationMinutes = settings.SprintDurationMinutes,
                ShortBreakDurationMinutes = settings.ShortBreakDurationMinutes,
                LongBreakDurationMinutes = settings.LongBreakDurationMinutes,
                DailySprintGoal = settings.DailySprintGoal
            };
            await _context.PomodoroSettings.AddAsync(row, cancellationToken);
        }
        else
        {
            row.SprintDurationMinutes = settings.SprintDurationMinutes;
            row.ShortBreakDurationMinutes = settings.ShortBreakDurationMinutes;
            row.LongBreakDurationMinutes = settings.LongBreakDurationMinutes;
            row.DailySprintGoal = settings.DailySprintGoal;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
