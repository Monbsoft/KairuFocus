using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kairudev.Infrastructure.Persistence;

internal sealed class KairudevDbContextFactory : IDesignTimeDbContextFactory<KairudevDbContext>
{
    public KairudevDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
        var builder = new DbContextOptionsBuilder<KairudevDbContext>();

        if (!string.IsNullOrWhiteSpace(connectionString))
            builder.UseSqlServer(connectionString);
        else
            builder.UseSqlite("Data Source=kairudev.db");

        return new KairudevDbContext(builder.Options);
    }
}
