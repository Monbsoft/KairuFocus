using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

/// <summary>
/// An isolated InMemory database per test. Seeding and querying use distinct contexts, so a query never
/// answers from the change tracker. InMemory enforces neither unique indexes nor foreign keys: uniqueness
/// conflicts are covered by the Application fakes, and the SQL shape by <see cref="SqlCommandCapture"/>.
/// </summary>
internal sealed class InMemoryDatabase
{
    private readonly DbContextOptions<KairuFocusDbContext> _options = new DbContextOptionsBuilder<KairuFocusDbContext>()
        .UseInMemoryDatabase("InfrastructureTests_" + Guid.NewGuid())
        .Options;

    public KairuFocusDbContext CreateContext() => new(_options);

    public async Task SeedAsync(params object[] entities)
    {
        await using var context = CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }
}
