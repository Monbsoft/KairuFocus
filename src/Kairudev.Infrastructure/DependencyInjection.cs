using Kairudev.Domain.Pomodoro;
using Kairudev.Domain.Tasks;
using Kairudev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kairudev.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<KairudevDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ITaskRepository, SqliteTaskRepository>();
        services.AddScoped<IPomodoroSessionRepository, SqlitePomodoroSessionRepository>();
        services.AddScoped<IPomodoroSettingsRepository, SqlitePomodoroSettingsRepository>();

        return services;
    }
}
