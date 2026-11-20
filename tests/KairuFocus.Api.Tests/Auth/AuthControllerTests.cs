using System.Net;
using System.Security.Claims;
using KairuFocus.Application.Identity.Commands.GetOrCreateUser;
using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monbsoft.BrilliantMediator.Abstractions;

namespace KairuFocus.Api.Tests.Auth;

/// <summary>
/// Integration tests on AuthController.
/// Verifies that the OAuth cookies are cleared on error paths,
/// in AuthController and on /signin-github.
/// </summary>
public sealed class AuthControllerTests : IClassFixture<KairuFocusApiFactory>
{
    private const string AuthCookieName = ".AspNetCore.Cookies";
    private const string CorrelationCookiePrefix = ".AspNetCore.Correlation.";

    private readonly KairuFocusApiFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(KairuFocusApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            // Disable automatic redirect following so we can inspect response headers.
            AllowAutoRedirect = false,
        });
    }

    /// <summary>
    /// Verifies that the GitHub callback, when the Cookies scheme cannot authenticate
    /// (no cookie, or a cookie that no longer decrypts), deletes the authentication cookie
    /// before redirecting with auth-error=denied.
    /// </summary>
    [Fact]
    public async Task Should_ClearCookie_When_CallbackAuthenticationFails()
    {
        // Arrange — direct call without going through the GitHub OAuth flow:
        // no authentication cookie → AuthenticateAsync(Cookies) does not succeed.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/github/callback");

        // Act
        var response = await _client.SendAsync(request);

        // Assert — redirect to the error page, with the authentication cookie expired.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost/login#auth-error=denied", response.Headers.Location?.ToString());
        Assert.Contains(SetCookieHeaders(response), c => IsDeletion(c, AuthCookieName));
    }

    /// <summary>
    /// Verifies that a failure on /signin-github (state the server can no longer read, e.g. after
    /// a Data Protection key change) redirects to the login error page instead of a 500,
    /// and deletes both the authentication cookie and the correlation cookie on its own path.
    /// </summary>
    [Fact]
    public async Task Should_RedirectWithDeniedErrorAndClearCookies_When_GitHubRemoteAuthenticationFails()
    {
        // Arrange — a real challenge issues the correlation cookie the browser sends back to /signin-github.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
        var challenge = await client.GetAsync("/api/auth/github");
        var correlationSetCookie = Assert.Single(
            SetCookieHeaders(challenge), c => c.StartsWith(CorrelationCookiePrefix, StringComparison.Ordinal));
        var correlationNameValue = correlationSetCookie[..correlationSetCookie.IndexOf(';')];
        var correlationName = correlationNameValue[..correlationNameValue.IndexOf('=')];

        var callback = new HttpRequestMessage(HttpMethod.Get, "/signin-github?code=fake-code&state=unreadable-state");
        callback.Headers.Add("Cookie", correlationNameValue);

        // Act
        var response = await client.SendAsync(callback);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost/login#auth-error=denied", response.Headers.Location?.ToString());

        var setCookies = SetCookieHeaders(response);
        Assert.Contains(setCookies, c => IsDeletion(c, AuthCookieName));
        var correlationDeletion = Assert.Single(setCookies, c => IsDeletion(c, correlationName));
        Assert.Equal(PathOf(correlationSetCookie), PathOf(correlationDeletion));
    }

    private static List<string> SetCookieHeaders(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    // A deletion is the cookie set to an empty value with an expiry in 1970 (ResponseCookies.Delete).
    private static bool IsDeletion(string setCookie, string cookieName) =>
        setCookie.StartsWith(cookieName + "=;", StringComparison.Ordinal)
        && setCookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);

    private static string? PathOf(string setCookie) =>
        setCookie.Split(';', StringSplitOptions.TrimEntries)
            .FirstOrDefault(attribute => attribute.StartsWith("path=", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Tests the no-id branch of GitHubCallback:
/// Cookie authentication succeeds but the principal has no NameIdentifier claim.
/// </summary>
public sealed class GitHubCallbackNoIdTests : IDisposable
{
    private readonly KairuFocusApiFactory _factory;
    private readonly HttpClient _client;
    private readonly TestClaimsProvider _claimsProvider = new();

    public GitHubCallbackNoIdTests()
    {
        var claimsProvider = _claimsProvider;

        var factory = _factory = new KairuFocusApiFactory
        {
            ConfigureTestServices = services =>
            {
                // Register TestClaimsProvider and TestCookieAuthHandler, then replace
                // IAuthenticationSchemeProvider so that "Cookies" uses TestCookieAuthHandler.
                services.AddSingleton(claimsProvider);
                services.AddTransient<TestCookieAuthHandler>();
                services.AddSingleton<IAuthenticationSchemeProvider>(sp =>
                {
                    var inner = new AuthenticationSchemeProvider(
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthenticationOptions>>());
                    return new TestAuthSchemeProvider(inner);
                });
            }
        };

        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>
    /// Verifies that when AuthenticateAsync(Cookie) succeeds but the principal
    /// has no NameIdentifier claim, the Cookie scheme is signed out and the callback
    /// redirects with auth-error=no-id.
    /// </summary>
    [Fact]
    public async Task Should_ClearCookieAndRedirectWithNoIdError_When_CallbackAuthenticatesWithoutGitHubId()
    {
        // Arrange — return a principal without NameIdentifier to trigger the no-id branch.
        _claimsProvider.Claims = [
            new Claim(ClaimTypes.Name, "test-user")
            // No ClaimTypes.NameIdentifier
        ];

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/github/callback");

        // Act
        var response = await _client.SendAsync(request);

        // Assert — must redirect to the no-id error page.
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect
            || response.StatusCode == HttpStatusCode.Found,
            $"Expected redirect, got {(int)response.StatusCode}");

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("auth-error=no-id", location, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _claimsProvider.SignOutCount);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}

/// <summary>
/// Tests the server error branch of GitHubCallback:
/// Cookie authentication succeeds and the principal has a NameIdentifier claim,
/// but GetOrCreateUser fails.
/// </summary>
public sealed class GitHubCallbackServerErrorTests : IDisposable
{
    private readonly KairuFocusApiFactory _factory;
    private readonly HttpClient _client;
    private readonly TestClaimsProvider _claimsProvider = new();

    public GitHubCallbackServerErrorTests()
    {
        var claimsProvider = _claimsProvider;
        var mediator = new TestMediator
        {
            DispatchResult = _ => Result.Failure<GetOrCreateUserResult>("simulated server error")
        };

        var factory = _factory = new KairuFocusApiFactory
        {
            ConfigureTestServices = services =>
            {
                // Register TestClaimsProvider and TestCookieAuthHandler, then replace
                // IAuthenticationSchemeProvider so that "Cookies" uses TestCookieAuthHandler.
                services.AddSingleton(claimsProvider);
                services.AddTransient<TestCookieAuthHandler>();
                services.AddSingleton<IAuthenticationSchemeProvider>(sp =>
                {
                    var inner = new AuthenticationSchemeProvider(
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthenticationOptions>>());
                    return new TestAuthSchemeProvider(inner);
                });

                // Replace IMediator with the test stub that returns a failure.
                services.RemoveAll<IMediator>();
                services.AddSingleton<IMediator>(mediator);
            }
        };

        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>
    /// Verifies that when AuthenticateAsync(Cookie) succeeds with a valid NameIdentifier
    /// but GetOrCreateUser returns a failure, the Cookie scheme is signed out and the callback
    /// redirects with auth-error=server.
    /// </summary>
    [Fact]
    public async Task Should_ClearCookieAndRedirectWithServerError_When_GetOrCreateUserFails()
    {
        // Arrange — return a principal with NameIdentifier so the controller reaches the mediator.
        _claimsProvider.Claims = [
            new Claim(ClaimTypes.NameIdentifier, "123456"),
            new Claim(ClaimTypes.Name, "test-user")
        ];

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/github/callback");

        // Act
        var response = await _client.SendAsync(request);

        // Assert — must redirect to the server error page.
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect
            || response.StatusCode == HttpStatusCode.Found,
            $"Expected redirect, got {(int)response.StatusCode}");

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("auth-error=server", location, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _claimsProvider.SignOutCount);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
