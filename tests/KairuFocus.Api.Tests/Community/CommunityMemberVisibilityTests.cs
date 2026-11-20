using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using KairuFocus.Api.Tests.Auth;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KairuFocus.Api.Tests.Community;

/// <summary>
/// End to end through the real handlers and repositories on an InMemory database (no mediator stub):
/// a member who is not activated cannot be told apart from an unknown name, and a profile is visible
/// only while its owner is a member.
/// </summary>
public sealed class CommunityMemberVisibilityTests : IDisposable
{
    private const string ChosenName = "RenardAgile";

    // One InMemory database per factory, shared by every scope and request (see KairuFocusApiFactory).
    private readonly KairuFocusApiFactory _factory = new();

    private readonly User _user = User.Create("gh-1", "octocat", "The Octocat", "octocat@example.com");
    private readonly HttpClient _client;

    public CommunityMemberVisibilityTests()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KairuFocusDbContext>();
            db.Users.Add(_user);
            db.SaveChanges();
        }

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(_user.Id.Value));
    }

    [Fact]
    public async Task Should_ReturnIdentical404_When_LoginBelongsToNonMemberOrNameIsUnknown()
    {
        var known = await _client.GetAsync("api/community/members/octocat");
        var unknown = await _client.GetAsync("api/community/members/NobodyHere");

        Assert.Equal(HttpStatusCode.NotFound, known.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(await BodyWithoutTraceId(unknown), await BodyWithoutTraceId(known));
    }

    [Fact]
    public async Task Should_ShowProfileOnlyWhileMember_When_JoiningThenLeaving()
    {
        var join = await _client.PostAsJsonAsync("api/community/membership", new { source = "Chosen", name = ChosenName });
        Assert.True(join.StatusCode == HttpStatusCode.Created, await join.Content.ReadAsStringAsync());

        var visible = await _client.GetAsync($"api/community/members/{ChosenName}");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        var json = await visible.Content.ReadAsStringAsync();
        Assert.Contains(ChosenName, json);
        Assert.DoesNotContain(_user.Id.Value.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);

        var leave = await _client.DeleteAsync("api/community/membership");
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);

        var gone = await _client.GetAsync($"api/community/members/{ChosenName}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    private static async Task<string> BodyWithoutTraceId(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var node = JsonNode.Parse(text)!.AsObject();
        node.Remove("traceId");
        return node.ToJsonString();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
