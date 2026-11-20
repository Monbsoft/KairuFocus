using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KairuFocus.Api.Tests.Auth;

/// <summary>
/// Holds the claims to return for a test, scoped per WebApplicationFactory instance,
/// and records whether the Cookie scheme was signed out.
/// Inject as a singleton in the DI container for each test factory.
/// </summary>
public sealed class TestClaimsProvider
{
    /// <summary>
    /// Claims to include in the authenticated principal.
    /// Set to null to simulate authentication failure (NoResult).
    /// </summary>
    public IEnumerable<Claim>? Claims { get; set; }

    /// <summary>Number of SignOutAsync calls received by the Cookie scheme.</summary>
    public int SignOutCount { get; private set; }

    internal void RecordSignOut() => SignOutCount++;
}

/// <summary>
/// Authentication handler used in integration tests to simulate the Cookie scheme
/// returning a successful AuthenticateResult with configurable claims.
///
/// Implements IAuthenticationSignOutHandler so that SignOutAsync("Cookies")
/// does not throw. There is no real cookie to clear: the sign-out is recorded
/// on <see cref="TestClaimsProvider"/> so tests can assert it happened.
///
/// Claims are provided via <see cref="TestClaimsProvider"/> injected from the DI container,
/// which avoids shared static state between concurrent test factories.
/// </summary>
public sealed class TestCookieAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>,
      IAuthenticationSignOutHandler
{
    private readonly TestClaimsProvider _claimsProvider;

    public TestCookieAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestClaimsProvider claimsProvider)
        : base(options, logger, encoder)
    {
        _claimsProvider = claimsProvider;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_claimsProvider.Claims is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(_claimsProvider.Claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Records the sign-out: in tests there is no real cookie to clear.
    /// </summary>
    public Task SignOutAsync(AuthenticationProperties? properties)
    {
        _claimsProvider.RecordSignOut();
        return Task.CompletedTask;
    }
}
