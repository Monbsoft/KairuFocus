using System.Net;
using KairuFocus.Api.Tests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KairuFocus.Api.Tests.Hosting;

/// <summary>
/// Integration tests on the canonical host: every request on www.kairufocus.com
/// must be permanently redirected to https://kairufocus.com, path and query kept.
///
/// Requests are sent over plain http with X-Forwarded-Proto: https, as Azure Container Apps
/// does after terminating TLS. The expected https:// in Location therefore also proves that
/// the redirect runs after UseForwardedHeaders.
/// </summary>
public sealed class WwwRedirectTests : IClassFixture<KairuFocusApiFactory>
{
    private readonly HttpClient _client;

    public WwwRedirectTests(KairuFocusApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Theory]
    [InlineData("/", "https://kairufocus.com/")]
    [InlineData("/tasks?filter=today", "https://kairufocus.com/tasks?filter=today")]
    [InlineData("/api/auth/github?returnUrl=%2Fjournal", "https://kairufocus.com/api/auth/github?returnUrl=%2Fjournal")]
    public async Task Should_RedirectPermanentlyToBareDomain_When_HostIsWww(string pathAndQuery, string expectedLocation)
    {
        var response = await _client.SendAsync(BehindAzureProxy("www.kairufocus.com", pathAndQuery));

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Should_NotRedirect_When_HostIsBareDomain()
    {
        var response = await _client.SendAsync(BehindAzureProxy("kairufocus.com", "/tasks?filter=today"));

        Assert.False(
            (int)response.StatusCode is >= 300 and < 400,
            $"Expected no redirect on the bare domain, got {(int)response.StatusCode} to {response.Headers.Location}.");
    }

    [Fact]
    public async Task Should_SendGitHubBackToBareDomain_When_LoginStartsOnBareDomain()
    {
        var response = await _client.SendAsync(BehindAzureProxy("kairufocus.com", "/api/auth/github"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.StartsWith("https://github.com/login/oauth/authorize", location);
        Assert.Contains("redirect_uri=https%3A%2F%2Fkairufocus.com%2Fsignin-github", location);
    }

    private static HttpRequestMessage BehindAzureProxy(string host, string pathAndQuery)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://{host}{pathAndQuery}");
        request.Headers.Add("X-Forwarded-Proto", "https");
        return request;
    }
}
