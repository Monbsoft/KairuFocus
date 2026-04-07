using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kairu.Infrastructure.Persistence;

internal sealed class KairuDbContextFactory : IDesignTimeDbContextFactory<KairuDbContext>
{
    public KairuDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=kairudev;Trusted_Connection=true;";

        var builder = new DbContextOptionsBuilder<KairuDbContext>();
        builder.UseSqlServer(connectionString);

        return new KairuDbContext(builder.Options);
    }
}
