using Kairu.Domain.Identity;
using Kairu.Domain.Pomodoro;
using Kairu.Infrastructure.Persistence.Internal;
using Microsoft.EntityFrameworkCore;

namespace Kairu.Infrastructure.Persistence;

internal sealed class EfCorePomodoroSettingsRepository : IPomodoroSettingsRepository
{
    private readonly KairuDbContext _context;

    public EfCorePomodoroSettingsRepository(KairuDbContext context)
    {
        _context = context;
    }

    public async Task<PomodoroSettings> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.PomodoroSettings
            .FirstOrDefaultAsync(s => s.UserId == userId.Value.ToString(), cancellationToken);

        if (row is null)
            return PomodoroSettings.Default;

        return PomodoroSettings.Create(
            row.SprintDurationMinutes,
            row.ShortBreakDurationMinutes,
            row.LongBreakDurationMinutes).Value;
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
                LongBreakDurationMinutes = settings.LongBreakDurationMinutes
            };
            await _context.PomodoroSettings.AddAsync(row, cancellationToken);
        }
        else
        {
            row.SprintDurationMinutes = settings.SprintDurationMinutes;
            row.ShortBreakDurationMinutes = settings.ShortBreakDurationMinutes;
            row.LongBreakDurationMinutes = settings.LongBreakDurationMinutes;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
