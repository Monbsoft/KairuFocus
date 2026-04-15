using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KairuFocus.Infrastructure.Persistence;

internal sealed class KairuFocusDbContextFactory : IDesignTimeDbContextFactory<KairuFocusDbContext>
{
    public KairuFocusDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=kairufocus;Trusted_Connection=true;";

        var builder = new DbContextOptionsBuilder<KairuFocusDbContext>();
        builder.UseSqlServer(connectionString);

        return new KairuFocusDbContext(builder.Options);
    }
}
