using Kairudev.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Kairudev.Infrastructure.Persistence.Repositories;

internal sealed class SqliteUserSettingsRepository : IUserSettingsRepository
{
    private readonly KairudevDbContext _context;

    public SqliteUserSettingsRepository(KairudevDbContext context)
    {
        _context = context;
    }

    public async Task<UserSettings> GetAsync()
    {
        var settings = await _context.Set<UserSettings>()
            .FirstOrDefaultAsync(s => s.Id == UserSettings.SingletonId);

        if (settings == null)
        {
            // Create default settings if none exist
            settings = UserSettings.CreateDefault();
            _context.Set<UserSettings>().Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task UpdateAsync(UserSettings settings)
    {
        _context.Set<UserSettings>().Update(settings);
        await _context.SaveChangesAsync();
    }
}
