using Kairudev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kairudev.IntegrationTests.Support;

public class DatabaseContext : IDisposable
{
    private readonly DbContextOptions<KairudevDbContext> _options;
    public KairudevDbContext DbContext { get; private set; }

    public DatabaseContext()
    {
        // Use in-memory SQLite for testing
        var builder = new DbContextOptionsBuilder<KairudevDbContext>()
            .UseSqlite("Data Source=:memory:");

        _options = builder.Options;

        DbContext = new KairudevDbContext(_options);
        // Don't use migrations, create tables directly from model
        EnsureTablesCreated();
    }

    private void EnsureTablesCreated()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }

    public void Reset()
    {
        DbContext?.Dispose();
        DbContext = new KairudevDbContext(_options);
        EnsureTablesCreated();
    }
}
