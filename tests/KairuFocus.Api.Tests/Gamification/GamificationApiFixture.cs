using KairuFocus.Api.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monbsoft.BrilliantMediator.Abstractions;

namespace KairuFocus.Api.Tests.Gamification;

/// <summary>
/// One API host per test class, with <see cref="IMediator"/> replaced by a <see cref="TestMediator"/>:
/// the tests cover the HTTP adapter only (routing, authentication, period parsing, error mapping, JSON contract).
/// xUnit runs the tests of a class sequentially, so each test configures the shared mediator itself.
/// </summary>
public sealed class GamificationApiFixture : IDisposable
{
    public TestMediator Mediator { get; } = new();

    public KairuFocusApiFactory Factory { get; }

    public GamificationApiFixture()
    {
        var mediator = Mediator;
        Factory = new KairuFocusApiFactory
        {
            ConfigureTestServices = services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton<IMediator>(mediator);
            }
        };
    }

    public void Dispose() => Factory.Dispose();
}
