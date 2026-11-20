using KairuFocus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KairuFocus.Api.Tests.Auth;

/// <summary>
/// WebApplicationFactory configured for integration tests.
/// - Uses "Testing" environment to disable SQL migration and prod validations.
/// - Replaces KairuFocusDbContext (SQL Server) with an InMemory provider, one database per factory.
/// - Accepts an optional <see cref="ConfigureTestServices"/> action to allow
///   per-test service overrides (e.g., replacing authentication handlers or the mediator).
/// </summary>
public sealed class KairuFocusApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Key that signs and validates JWTs in tests. Test-only: production code has no fallback key.
    /// </summary>
    public const string JwtSecretKey = "kairufocus-integration-tests-signing-key";

    // Fixed once per factory: a name computed inside the options lambda would give each scope
    // (so each request) its own empty database.
    private readonly string _databaseName = "KairuFocusTests_" + Guid.NewGuid();

    /// <summary>
    /// Optional additional service configuration applied after the base setup.
    /// Used by tests that need to override specific services (e.g., Cookie auth handler, IMediator).
    /// </summary>
    public Action<IServiceCollection>? ConfigureTestServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" environment: disables EF migration, GitHub secret validation,
        // and prod Data Protection fallback.
        builder.UseEnvironment("Testing");

        // Values read by Program.cs while it builds the services. UseSetting reaches the host configuration,
        // which WebApplication.CreateBuilder sees immediately; ConfigureAppConfiguration below is only applied
        // once Program.cs calls Build(), too late for these reads.
        builder.UseSetting("Jwt:SecretKey", JwtSecretKey);
        builder.UseSetting("GitHub:ClientId", "test-client-id");
        builder.UseSetting("GitHub:ClientSecret", "test-client-secret");
        // Never opened: the SQL Server options are replaced by InMemory below.
        builder.UseSetting("ConnectionStrings:Default", "Server=unused-in-tests;Database=KairuFocus_Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Values read per request (IConfiguration resolved after Build()).
            var testConfig = new Dictionary<string, string?>
            {
                ["WebBaseUrl"] = "http://localhost",
                ["AllowedCallbackUrls:0"] = "http://localhost/callback",
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server DbContext with InMemory for tests. Since EF Core 9, AddDbContext also registers
            // an IDbContextOptionsConfiguration<TContext> holding UseSqlServer: left in place, it would register
            // a second provider next to InMemory. The interface is EF internal API, so it is matched by name.
            foreach (var descriptor in services
                .Where(d => d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)
                    && d.ServiceType.GenericTypeArguments.SequenceEqual([typeof(KairuFocusDbContext)]))
                .ToList())
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<DbContextOptions<KairuFocusDbContext>>();
            services.RemoveAll<KairuFocusDbContext>();

            services.AddDbContext<KairuFocusDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Apply per-test overrides (e.g., auth handler replacement, mediator stub).
            ConfigureTestServices?.Invoke(services);
        });
    }
}
