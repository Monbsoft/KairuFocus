using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using KairuFocus.Api.Tests.Auth;
using KairuFocus.Api.Tests.Gamification;
using KairuFocus.Application.Community.Commands.ChangeProfileName;
using KairuFocus.Application.Community.Commands.JoinCommunity;
using KairuFocus.Application.Community.Commands.JoinOpenSprint;
using KairuFocus.Application.Community.Commands.LeaveCommunity;
using KairuFocus.Application.Community.Commands.SetSprintClosed;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetCommunityMembership;
using KairuFocus.Application.Community.Queries.GetMemberProfile;
using KairuFocus.Application.Community.Queries.GetOpenSprints;
using KairuFocus.Application.Community.Queries.GetSprintParticipants;
using KairuFocus.Application.Pomodoro.Commands.StartSession;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Application.Pomodoro.Queries.GetCurrentSession;
using KairuFocus.Domain.Community;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Api.Tests.Community;

/// <summary>
/// HTTP adapter of the community (plan #44 §6.5 and §8): authentication, body parsing, error mapping
/// and the exact JSON contracts. The use cases themselves are covered by the Application tests.
/// </summary>
public sealed class CommunityEndpointsTests : IClassFixture<GamificationApiFixture>
{
    private const string MembershipUrl = "api/community/membership";
    private const string MembershipNameUrl = "api/community/membership/name";
    private const string MemberUrl = "api/community/members/RenardAgile";
    private const string OpenSprintsUrl = "api/community/open-sprints";
    private const string JoinUrl = "api/community/open-sprints/LoutreTenace/join";
    private const string ParticipantsUrl = "api/community/current-sprint/participants";
    private const string ClosedUrl = "api/community/current-sprint/closed";
    private const string SessionUrl = "api/pomodoro/session";

    private const string NameUnavailableText = "This name is not available.";

    private static readonly string[] ForbiddenKeys =
        ["userId", "ownerId", "email", "displayName", "id", "sessionId", "rootSessionId", "sharedSprintId"];

    private static readonly DateTimeOffset SharedEnd = new(2026, 10, 30, 14, 25, 0, TimeSpan.Zero);

    private readonly TestMediator _mediator;
    private readonly HttpClient _client;
    private readonly HttpClient _anonymousClient;

    public CommunityEndpointsTests(GamificationApiFixture fixture)
    {
        // The fixture's mediator is shared by the tests of this class: start each test unconfigured,
        // so that an unexpected mediator call fails the request instead of returning a stale result.
        _mediator = fixture.Mediator;
        _mediator.SendResult = null;
        _mediator.DispatchResult = null;

        _anonymousClient = fixture.Factory.CreateClient();
        _client = fixture.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(Guid.NewGuid()));
    }

    // ── Authentication ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("GET", MembershipUrl)]
    [InlineData("POST", MembershipUrl)]
    [InlineData("PUT", MembershipNameUrl)]
    [InlineData("DELETE", MembershipUrl)]
    [InlineData("GET", MemberUrl)]
    [InlineData("GET", OpenSprintsUrl)]
    [InlineData("POST", JoinUrl)]
    [InlineData("GET", ParticipantsUrl)]
    [InlineData("PUT", ClosedUrl)]
    public async Task Should_Return401_When_CommunityEndpointIsCalledAnonymously(string method, string url)
    {
        var response = await _anonymousClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Membership ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnMembershipContract_When_MembershipIsRequested()
    {
        _mediator.SendResult = _ => SampleMembership;

        var response = await _client.GetAsync(MembershipUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync("""
            {
              "isMember": true, "name": "RenardAgile", "nameSource": "GitHub",
              "avatarUrl": "https://avatars.githubusercontent.com/u/1",
              "gitHubLogin": "RenardAgile", "gitHubLoginAvailable": true
            }
            """, response);
    }

    [Fact]
    public async Task Should_Return201WithMembership_When_CommunityIsJoined()
    {
        JoinCommunityCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (JoinCommunityCommand)command;
            return JoinCommunityResult.Success(SampleMembership);
        };

        var response = await PostJsonAsync(MembershipUrl, """{ "source": "Chosen", "name": "RenardAgile" }""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(new JoinCommunityCommand(ProfileNameSource.Chosen, "RenardAgile"), sent);
        Assert.Equal("RenardAgile", (await ReadJsonAsync(response))["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Should_Return200WithMembership_When_NameIsChanged()
    {
        ChangeProfileNameCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (ChangeProfileNameCommand)command;
            return ChangeProfileNameResult.Success(SampleMembership);
        };

        var response = await PutJsonAsync(MembershipNameUrl, """{ "source": "github" }""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new ChangeProfileNameCommand(ProfileNameSource.GitHub, null), sent);
        Assert.Equal("RenardAgile", (await ReadJsonAsync(response))["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Should_Return204_When_CommunityIsLeft()
    {
        _mediator.DispatchResult = _ => LeaveCommunityResult.Success();

        var response = await _client.DeleteAsync(MembershipUrl);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("""{ "name": "RenardAgile" }""")]
    [InlineData("""{ "source": null, "name": "RenardAgile" }""")]
    [InlineData("""{ "source": "Unknown", "name": "RenardAgile" }""")]
    [InlineData("""{ "source": "1", "name": "RenardAgile" }""")]
    [InlineData("""{ "source": "GitHub,Chosen", "name": "RenardAgile" }""")]
    public async Task Should_Return400InvalidName_When_SourceIsMissingOrUnknown(string body)
    {
        var dispatched = false;
        _mediator.DispatchResult = _ =>
        {
            dispatched = true;
            return JoinCommunityResult.Success(SampleMembership);
        };

        var join = await PostJsonAsync(MembershipUrl, body);
        var rename = await PutJsonAsync(MembershipNameUrl, body);

        foreach (var response in new[] { join, rename })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertJsonAsync(
                $$"""{ "error": "{{CommunityErrors.Community.InvalidName}}", "code": "invalid-name" }""", response);
        }
        Assert.False(dispatched);
    }

    // ── Error mapping (§8) ─────────────────────────────────────────────────

    [Theory]
    [InlineData(CommunityErrors.Community.InvalidName, HttpStatusCode.BadRequest, "invalid-name")]
    [InlineData(CommunityErrors.Community.ReservedName, HttpStatusCode.BadRequest, "name-unavailable")]
    [InlineData(CommunityErrors.Community.LoginOfAnotherUser, HttpStatusCode.BadRequest, "name-unavailable")]
    [InlineData(CommunityErrors.Community.NameTaken, HttpStatusCode.Conflict, "name-unavailable")]
    [InlineData(CommunityErrors.Community.AlreadyMember, HttpStatusCode.Conflict, "already-member")]
    public async Task Should_MapError_When_JoinCommunityFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => JoinCommunityResult.Failure(error);

        var response = await PostJsonAsync(MembershipUrl, """{ "source": "Chosen", "name": "x" }""");

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
    }

    [Theory]
    [InlineData(CommunityErrors.Community.InvalidName, HttpStatusCode.BadRequest, "invalid-name")]
    [InlineData(CommunityErrors.Community.ReservedName, HttpStatusCode.BadRequest, "name-unavailable")]
    [InlineData(CommunityErrors.Community.LoginOfAnotherUser, HttpStatusCode.BadRequest, "name-unavailable")]
    [InlineData(CommunityErrors.Community.NameTaken, HttpStatusCode.Conflict, "name-unavailable")]
    [InlineData(CommunityErrors.Community.NotMember, HttpStatusCode.NotFound, "not-member")]
    public async Task Should_MapError_When_ChangeNameFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => ChangeProfileNameResult.Failure(error);

        var response = await PutJsonAsync(MembershipNameUrl, """{ "source": "Chosen", "name": "x" }""");

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
    }

    [Fact]
    public async Task Should_Return404NotMember_When_LeavingWithoutMembership()
    {
        _mediator.DispatchResult = _ => LeaveCommunityResult.Failure(CommunityErrors.Community.NotMember);

        var response = await _client.DeleteAsync(MembershipUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertCodeAsync("not-member", response);
    }

    [Theory]
    [InlineData(CommunityErrors.Community.NotMember, HttpStatusCode.Forbidden, "not-member")]
    [InlineData(PomodoroErrors.Pomodoro.SessionAlreadyActive, HttpStatusCode.Conflict, "sprint-active")]
    [InlineData(PomodoroErrors.Pomodoro.ConcurrentSessionStart, HttpStatusCode.Conflict, "sprint-active")]
    [InlineData(CommunityErrors.Community.CannotJoinOwnSprint, HttpStatusCode.Conflict, "own-sprint")]
    [InlineData(CommunityErrors.Community.SprintNotJoinable, HttpStatusCode.Gone, "sprint-closed")]
    public async Task Should_MapError_When_JoinOpenSprintFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => JoinOpenSprintResult.Failure(error);

        var response = await _client.PostAsync(JoinUrl, content: null);

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
    }

    [Theory]
    [InlineData(CommunityErrors.Community.NotSprintHost, HttpStatusCode.Forbidden, "not-host")]
    [InlineData(CommunityErrors.Community.NoActiveSprint, HttpStatusCode.NotFound, "no-active-sprint")]
    public async Task Should_MapError_When_SetSprintClosedFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => SetSprintClosedResult.Failure(error);

        var response = await PutJsonAsync(ClosedUrl, """{ "closed": true }""");

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
    }

    [Fact]
    public async Task Should_Return500_When_ErrorIsNotInTheContract()
    {
        _mediator.DispatchResult = _ => JoinCommunityResult.Failure(CommunityErrors.Community.UserNotFound);

        var response = await PostJsonAsync(MembershipUrl, """{ "source": "GitHub" }""");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Null(body["code"]);
        Assert.NotNull(body["error"]);
    }

    [Fact]
    public async Task Should_ReturnSameBody_When_NameIsReservedOrAnotherUsersLogin()
    {
        var bodies = new List<string>();
        foreach (var error in new[]
                 {
                     CommunityErrors.Community.ReservedName,
                     CommunityErrors.Community.LoginOfAnotherUser,
                     CommunityErrors.Community.NameTaken
                 })
        {
            _mediator.DispatchResult = _ => JoinCommunityResult.Failure(error);
            var response = await PostJsonAsync(MembershipUrl, """{ "source": "Chosen", "name": "admin" }""");
            bodies.Add(await response.Content.ReadAsStringAsync());
        }

        Assert.Single(bodies.Distinct());
        Assert.Equal(NameUnavailableText, JsonNode.Parse(bodies[0])!["error"]!.GetValue<string>());
    }

    // ── Member profile ─────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ExposeOnlyPublicFields_When_MemberProfileIsReturned()
    {
        _mediator.SendResult = _ => new GetMemberProfileResult(true, SampleProfile);

        var response = await _client.GetAsync(MemberUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actual = await ReadJsonAsync(response);
        AssertNoForbiddenKey(actual);
        await AssertJsonAsync("""
            {
              "name": "RenardAgile", "isGitHub": true, "avatarUrl": "https://avatars.githubusercontent.com/u/1",
              "joinedMonth": "2026-07-01", "isMe": false, "viewerIsMember": true,
              "standing": { "xp": 1240, "rank": 4 }, "standingFailed": false,
              "activity": {
                "sprints": 124, "focusMinutes": 3100, "sprintsTogether": 20,
                "days": [ { "date": "2026-10-29", "sprints": 3 } ]
              },
              "activityFailed": false,
              "openSprint": {
                "endsAt": "2026-10-30T14:25:00+00:00",
                "others": [ { "name": "LoutreTenace", "avatarUrl": null } ],
                "includesViewer": false
              },
              "openSprintFailed": false
            }
            """, response);
    }

    [Fact]
    public async Task Should_ReturnIdentical404_When_NameIsUnknownOrMalformed()
    {
        _mediator.SendResult = _ => new GetMemberProfileResult(false, null);

        var unknown = await _client.GetAsync("api/community/members/NobodyHere");
        var malformed = await _client.GetAsync("api/community/members/%21%21");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
        // The framework's problem details carry a per-request traceId: the rest of the body must be identical.
        var unknownBody = (JsonObject)await ReadJsonAsync(unknown);
        var malformedBody = (JsonObject)await ReadJsonAsync(malformed);
        unknownBody.Remove("traceId");
        malformedBody.Remove("traceId");
        Assert.True(JsonNode.DeepEquals(unknownBody, malformedBody));
        Assert.Equal(unknown.Content.Headers.ContentType, malformed.Content.Headers.ContentType);
    }

    // ── Open sprints, participants ─────────────────────────────────────────

    [Fact]
    public async Task Should_ExposeOnlyPublicFields_When_OpenSprintsAreReturned()
    {
        _mediator.SendResult = _ => new GetOpenSprintsResult(
            ViewerIsMember: true,
            Groups: [new OpenSprintRowViewModel("LoutreTenace", null, SharedEnd, 3, IsMine: false)]);

        var response = await _client.GetAsync(OpenSprintsUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoForbiddenKey(await ReadJsonAsync(response));
        await AssertJsonAsync("""
            {
              "viewerIsMember": true,
              "groups": [ {
                "name": "LoutreTenace", "avatarUrl": null, "endsAt": "2026-10-30T14:25:00+00:00",
                "participantCount": 3, "isMine": false
              } ]
            }
            """, response);
    }

    [Fact]
    public async Task Should_ExposeOnlyPublicFields_When_ParticipantsAreReturned()
    {
        _mediator.SendResult = _ => new GetSprintParticipantsResult(
            [new SprintMemberViewModel("LoutreTenace", "https://avatars.githubusercontent.com/u/2")]);

        var response = await _client.GetAsync(ParticipantsUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoForbiddenKey(await ReadJsonAsync(response));
        await AssertJsonAsync(
            """{ "others": [ { "name": "LoutreTenace", "avatarUrl": "https://avatars.githubusercontent.com/u/2" } ] }""",
            response);
    }

    // ── Join an open sprint, close a sprint ────────────────────────────────

    [Fact]
    public async Task Should_Return201WithSharedEnd_When_SprintIsJoined()
    {
        JoinOpenSprintCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (JoinOpenSprintCommand)command;
            return JoinOpenSprintResult.Success(SharedEnd);
        };

        var response = await _client.PostAsync(JoinUrl, content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(new JoinOpenSprintCommand("LoutreTenace"), sent);
        await AssertJsonAsync("""{ "sharedEndAt": "2026-10-30T14:25:00+00:00" }""", response);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Return204AndSendFlag_When_SprintClosedStateIsSet(bool closed)
    {
        SetSprintClosedCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (SetSprintClosedCommand)command;
            return SetSprintClosedResult.Success();
        };

        var response = await PutJsonAsync(ClosedUrl, $$"""{ "closed": {{closed.ToString().ToLowerInvariant()}} }""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(new SetSprintClosedCommand(closed), sent);
    }

    [Fact]
    public async Task Should_Return400_When_ClosedFlagIsMissing()
    {
        _mediator.DispatchResult = _ => SetSprintClosedResult.Success();

        var response = await PutJsonAsync(ClosedUrl, "{}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Pomodoro session contract ──────────────────────────────────────────

    [Fact]
    public async Task Should_ExposeSharedEndAndHostFlags_When_SessionIsReturned()
    {
        var session = new PomodoroSessionViewModel(
            Guid.NewGuid(), "Sprint", "Active", 25,
            StartedAt: new DateTime(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc), EndedAt: null,
            LinkedTaskIds: [], DurationSeconds: null,
            SharedEndAt: new DateTime(2026, 10, 30, 14, 25, 0, DateTimeKind.Utc),
            IsGroupHost: true, IsClosedToJoin: true);
        _mediator.SendResult = _ => GetCurrentSessionResult.WithSession(session);
        _mediator.DispatchResult = _ => StartSessionResult.Success(session);

        var get = await _client.GetAsync(SessionUrl);
        var post = await _client.PostAsync(SessionUrl + "?type=Sprint", content: null);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        foreach (var response in new[] { get, post })
        {
            var body = await ReadJsonAsync(response);
            Assert.Equal("2026-10-30T14:25:00Z", body["sharedEndAt"]!.GetValue<string>());
            Assert.True(body["isGroupHost"]!.GetValue<bool>());
            Assert.True(body["isClosedToJoin"]!.GetValue<bool>());
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static readonly MembershipViewModel SampleMembership = new(
        IsMember: true,
        Name: "RenardAgile",
        NameSource: "GitHub",
        AvatarUrl: "https://avatars.githubusercontent.com/u/1",
        GitHubLogin: "RenardAgile",
        GitHubLoginAvailable: true);

    private static readonly MemberProfileViewModel SampleProfile = new(
        Name: "RenardAgile",
        IsGitHub: true,
        AvatarUrl: "https://avatars.githubusercontent.com/u/1",
        JoinedMonth: new DateOnly(2026, 7, 1),
        IsMe: false,
        ViewerIsMember: true,
        Standing: new MemberStandingViewModel(1240, 4),
        StandingFailed: false,
        Activity: new MemberActivityViewModel(124, 3100, 20, [new ActivityDayViewModel(new DateOnly(2026, 10, 29), 3)]),
        ActivityFailed: false,
        OpenSprint: new OpenSprintStatusViewModel(SharedEnd, [new SprintMemberViewModel("LoutreTenace", null)], IncludesViewer: false),
        OpenSprintFailed: false);

    private Task<HttpResponseMessage> PostJsonAsync(string url, string json) =>
        _client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    private Task<HttpResponseMessage> PutJsonAsync(string url, string json) =>
        _client.PutAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

    /// <summary>The body is <c>{ error, code }</c>; the error text of a name refusal never says why.</summary>
    private static async Task AssertCodeAsync(string code, HttpResponseMessage response)
    {
        var body = (JsonObject)await ReadJsonAsync(response);
        Assert.Equal(["code", "error"], body.Select(p => p.Key).Order().ToArray());
        Assert.Equal(code, body["code"]!.GetValue<string>());
        if (code == "name-unavailable")
            Assert.Equal(NameUnavailableText, body["error"]!.GetValue<string>());
    }

    /// <summary>No property of a cross-user read may identify a user or a session, at any depth.</summary>
    private static void AssertNoForbiddenKey(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    Assert.DoesNotContain(key, ForbiddenKeys, StringComparer.OrdinalIgnoreCase);
                    AssertNoForbiddenKey(value);
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                    AssertNoForbiddenKey(item);
                break;
        }
    }

    /// <summary>Exact structural comparison: same property names, values and no extra property.</summary>
    private static async Task AssertJsonAsync(string expectedJson, HttpResponseMessage response)
    {
        var actualJson = await response.Content.ReadAsStringAsync();

        Assert.True(
            JsonNode.DeepEquals(JsonNode.Parse(expectedJson), JsonNode.Parse(actualJson)),
            $"Expected JSON:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual JSON:{Environment.NewLine}{actualJson}");
    }
}
