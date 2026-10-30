using KairuFocus.Api.Tests.Auth;
using KairuFocus.Domain.Community;
using Microsoft.Extensions.DependencyInjection;

namespace KairuFocus.Api.Tests.Community;

/// <summary>Real service graph (no mediator stub): nothing else checks that the community ports are registered.</summary>
public sealed class CommunityWiringTests : IDisposable
{
    private readonly KairuFocusApiFactory _factory = new();

    [Fact]
    public void Should_ResolveCommunityRepositories_When_ServicesAreBuilt()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.NotNull(provider.GetRequiredService<IMemberProfileRepository>());
        Assert.NotNull(provider.GetRequiredService<IMemberDirectoryReadRepository>());
        Assert.NotNull(provider.GetRequiredService<IOpenSprintReadRepository>());
    }

    public void Dispose() => _factory.Dispose();
}
