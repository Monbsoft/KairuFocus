using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using KairuFocus.Api.Tests.Auth;
using KairuFocus.Api.Tests.Gamification;
using KairuFocus.Application.Community.Commands.ChangeProfileName;
using KairuFocus.Application.Community.Commands.ChooseAmbiance;
using KairuFocus.Application.Community.Commands.ConsentToPresence;
using KairuFocus.Application.Community.Commands.RecordPresence;
using KairuFocus.Application.Community.Queries.GetAmbiances;
using KairuFocus.Application.Community.Queries.GetOpenSpace;
using KairuFocus.Application.Community.Commands.JoinCommunity;
using KairuFocus.Application.Community.Commands.FollowGroupHost;
using KairuFocus.Application.Community.Commands.JoinOpenSprint;
using KairuFocus.Application.Community.Commands.LaunchGroupStep;
using KairuFocus.Application.Community.Commands.LeaveCommunity;
using KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;
using KairuFocus.Application.Community.Commands.SetSprintClosed;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetCommunityMembership;
using KairuFocus.Application.Community.Queries.GetMemberProfile;
using KairuFocus.Application.Community.Queries.GetCurrentSession;
using KairuFocus.Application.Pomodoro.Commands.StartSession;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Community;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

using PomodoroGetCurrentSessionResult = KairuFocus.Application.Pomodoro.Queries.GetCurrentSession.GetCurrentSessionResult;

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
    private const string JoinUrl = "api/community/open-sprints/LoutreTenace/join";
    private const string CurrentSessionUrl = "api/community/current-session";
    private const string StepsUrl = "api/community/current-session/steps";
    private const string FollowUrl = "api/community/current-session/follow";
    private const string ClosedUrl = "api/community/current-sprint/closed";
    private const string PresenceUrl = "api/community/presence";
    private const string ConsentUrl = "api/community/presence/consent";
    private const string AmbianceUrl = "api/community/membership/ambiance";
    private const string AmbiancesUrl = "api/community/ambiances";
    private const string OpenSpaceUrl = "api/community/open-space";
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
    [InlineData("POST", JoinUrl)]
    [InlineData("GET", CurrentSessionUrl)]
    [InlineData("POST", StepsUrl)]
    [InlineData("POST", FollowUrl)]
    [InlineData("PUT", ClosedUrl)]
    [InlineData("POST", PresenceUrl)]
    [InlineData("PUT", ConsentUrl)]
    [InlineData("PUT", AmbianceUrl)]
    [InlineData("GET", AmbiancesUrl)]
    [InlineData("GET", OpenSpaceUrl)]
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
              "gitHubLogin": "RenardAgile", "gitHubLoginAvailable": true,
              "presenceConsented": true, "ambianceCode": null
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
    [InlineData(CommunityErrors.Community.GroupEnded, HttpStatusCode.Gone, "group-ended")]
    [InlineData(CommunityErrors.Community.GroupClosed, HttpStatusCode.Gone, "sprint-closed")]
    [InlineData(CommunityErrors.Community.GroupStepRunning, HttpStatusCode.Conflict, "step-running")]
    [InlineData(CommunityErrors.Community.AlreadyInGroup, HttpStatusCode.Conflict, "already-in-group")]
    [InlineData(CommunityErrors.Community.NotInGroup, HttpStatusCode.NotFound, "not-in-group")]
    [InlineData(CommunityErrors.Community.InvalidGroupStep, HttpStatusCode.BadRequest, "invalid-step")]
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
    public async Task Should_ReturnSameBody_When_NameIsReservedOrTaken()
    {
        var bodies = new List<string>();
        foreach (var error in new[]
                 {
                     CommunityErrors.Community.ReservedName,
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
                "includesViewer": false,
                "stepType": "Sprint",
                "phase": "Running"
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
    public async Task Should_Return204_When_NoSession()
    {
        _mediator.SendResult = _ => null!;

        var response = await _client.GetAsync(CurrentSessionUrl);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnJsonWithoutIds_When_InGroup()
    {
        _mediator.SendResult = _ => new GetCurrentSessionResult(
            IsGroup: true, Role: "Participant", Phase: "WaitingForHost", StepNumber: 2, SprintNumber: 1,
            StepType: "ShortBreak", StepEndsAt: SharedEnd, WaitLimitAt: SharedEnd.AddMinutes(15), EndReason: null,
            Others: [new SprintMemberViewModel("LoutreTenace", "https://avatars.githubusercontent.com/u/2")],
            IsClosedToJoin: false, Ambiance: null);

        var response = await _client.GetAsync(CurrentSessionUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        AssertNoForbiddenKey(body);
        Assert.Null(body["hostId"]);
        Assert.Null(body["userId"]);
        Assert.Equal("WaitingForHost", body["phase"]!.GetValue<string>());
        Assert.Equal("LoutreTenace", body["others"]![0]!["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Should_Return401_When_LeavingGroupSessionAnonymously()
    {
        var response = await _anonymousClient.DeleteAsync(CurrentSessionUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Return204_When_GroupSessionIsLeftOrStopped(bool stopped)
    {
        LeaveOrStopGroupSessionCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (LeaveOrStopGroupSessionCommand)command;
            return LeaveOrStopGroupSessionResult.Success(stopped);
        };

        var response = await _client.DeleteAsync(CurrentSessionUrl);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(new LeaveOrStopGroupSessionCommand(), sent);
    }

    [Theory]
    [InlineData(CommunityErrors.Community.NotInGroup, HttpStatusCode.NotFound, "not-in-group")]
    [InlineData(CommunityErrors.Community.GroupStepRunning, HttpStatusCode.Conflict, "step-running")]
    [InlineData(CommunityErrors.Community.NotMember, HttpStatusCode.Forbidden, "not-member")]
    public async Task Should_MapError_When_LeavingGroupSessionFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => LeaveOrStopGroupSessionResult.Failure(error);

        var response = await _client.DeleteAsync(CurrentSessionUrl);

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
    }

    [Fact]
    public async Task Should_Return410WaitingForHost_When_JoiningWaitingGroup()
    {
        _mediator.DispatchResult = _ => JoinOpenSprintResult.Failure(CommunityErrors.Community.GroupWaitingForHost);

        var response = await _client.PostAsync(JoinUrl, content: null);

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        await AssertCodeAsync("waiting-for-host", response);
    }

    // ── Launch a step, follow the host (UC-CO-10, UC-CO-14) ────────────────

    [Fact]
    public async Task Should_Return201WithStepEnd_When_StepIsLaunched()
    {
        LaunchGroupStepCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (LaunchGroupStepCommand)command;
            return LaunchGroupStepResult.Success(SharedEnd);
        };

        var response = await PostJsonAsync(StepsUrl, """{ "type": "ShortBreak" }""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(new LaunchGroupStepCommand("ShortBreak"), sent);
        await AssertJsonAsync("""{ "stepEndsAt": "2026-10-30T14:25:00+00:00" }""", response);
    }

    [Fact]
    public async Task Should_Return204_When_HostIsLeftAlone()
    {
        _mediator.DispatchResult = _ => LaunchGroupStepResult.EndedHostAlone();

        var response = await PostJsonAsync(StepsUrl, """{ "type": "Sprint" }""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(CommunityErrors.Community.GroupStepRunning, HttpStatusCode.Conflict, "step-running")]
    [InlineData(PomodoroErrors.Pomodoro.SessionAlreadyActive, HttpStatusCode.Conflict, "sprint-active")]
    [InlineData(CommunityErrors.Community.GroupEnded, HttpStatusCode.Gone, "group-ended")]
    [InlineData(CommunityErrors.Community.NotMember, HttpStatusCode.Forbidden, "not-member")]
    [InlineData(CommunityErrors.Community.NotSprintHost, HttpStatusCode.Forbidden, "not-host")]
    [InlineData(CommunityErrors.Community.InvalidGroupStep, HttpStatusCode.BadRequest, "invalid-step")]
    public async Task Should_MapError_When_LaunchStepFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => LaunchGroupStepResult.Failure(error);

        var response = await PostJsonAsync(StepsUrl, """{ "type": "Sprint" }""");

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "type": null }""")]
    public async Task Should_Return400_When_StepTypeIsMissing(string body)
    {
        var dispatched = false;
        _mediator.DispatchResult = _ =>
        {
            dispatched = true;
            return LaunchGroupStepResult.Success(SharedEnd);
        };

        var response = await PostJsonAsync(StepsUrl, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(dispatched);
    }

    [Fact]
    public async Task Should_Return200WithOutcome_When_FollowingStartsNewStep()
    {
        FollowGroupHostCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (FollowGroupHostCommand)command;
            return FollowGroupHostResult.NewStep("Sprint", SharedEnd, xpAwarded: 0);
        };

        var response = await _client.PostAsync(FollowUrl, content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(sent);
        await AssertJsonAsync("""
            {"outcome":"NewStep","stepType":"Sprint","stepEndsAt":"2026-10-30T14:25:00+00:00","waitLimitAt":null,"endReason":null,"xpAwarded":0}
            """, response);
    }

    public static TheoryData<string> FollowOutcomes => new()
    {
        FollowGroupHostResult.NewStepOutcome,
        FollowGroupHostResult.SameStepOutcome,
        FollowGroupHostResult.WaitingOutcome,
        FollowGroupHostResult.EndedOutcome,
        FollowGroupHostResult.NotInGroupOutcome,
    };

    [Theory]
    [MemberData(nameof(FollowOutcomes))]
    public async Task Should_Return200WithoutIds_When_FollowingGivesAnyOutcome(string outcome)
    {
        _mediator.DispatchResult = _ => outcome switch
        {
            FollowGroupHostResult.NewStepOutcome => FollowGroupHostResult.NewStep("Sprint", SharedEnd, 10),
            FollowGroupHostResult.SameStepOutcome => FollowGroupHostResult.SameStep("ShortBreak", SharedEnd),
            FollowGroupHostResult.WaitingOutcome =>
                FollowGroupHostResult.Waiting("Sprint", SharedEnd, SharedEnd.AddMinutes(15)),
            FollowGroupHostResult.EndedOutcome => FollowGroupHostResult.Ended("Stopped", 10),
            _ => FollowGroupHostResult.NotInGroup(),
        };

        var response = await _client.PostAsync(FollowUrl, content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        AssertNoForbiddenKey(body);
        Assert.Null(body["hostId"]);
        Assert.Null(body["isSuccess"]);
        Assert.Null(body["error"]);
        Assert.Equal(outcome, body["outcome"]!.GetValue<string>());
    }

    [Fact]
    public async Task Should_ReturnEndReasonAndXp_When_FollowingEndedGroup()
    {
        _mediator.DispatchResult = _ => FollowGroupHostResult.Ended("Stopped", 10);

        var response = await _client.PostAsync(FollowUrl, content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Ended", body["outcome"]!.GetValue<string>());
        Assert.Equal("Stopped", body["endReason"]!.GetValue<string>());
        Assert.Equal(10, body["xpAwarded"]!.GetValue<int>());
    }

    [Theory]
    [InlineData(CommunityErrors.Community.NotMember, HttpStatusCode.Forbidden, "not-member")]
    [InlineData(PomodoroErrors.Pomodoro.SessionAlreadyActive, HttpStatusCode.Conflict, "sprint-active")]
    public async Task Should_MapError_When_FollowingFails(string error, HttpStatusCode status, string code)
    {
        _mediator.DispatchResult = _ => FollowGroupHostResult.Failure(error);

        var response = await _client.PostAsync(FollowUrl, content: null);

        Assert.Equal(status, response.StatusCode);
        await AssertCodeAsync(code, response);
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

    // The common break of #45 is replaced by the steps the host launches (#46): the former route answers like
    // a route that never existed (the host's fallback decides the status), and reaches no handler.
    [Fact]
    public async Task Should_AnswerLikeUnknownRoute_When_FormerGroupBreakRouteIsCalled()
    {
        var former = await _client.PostAsync("api/community/current-sprint/break", content: null);
        var unknown = await _client.PostAsync("api/community/current-sprint/never-existed", content: null);

        Assert.False(former.IsSuccessStatusCode);
        Assert.Equal(unknown.StatusCode, former.StatusCode);
    }

    // ── Open space, presence, ambiance (#50) ───────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Return204_When_PresenceRecorded(bool recorded)
    {
        RecordPresenceCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (RecordPresenceCommand)command;
            return new RecordPresenceResult(recorded);
        };

        var response = await _client.PostAsync(PresenceUrl, content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(sent);
    }

    [Fact]
    public async Task Should_Return204_When_PresenceIgnored()
    {
        // Not consented or not a member: the heartbeat is silently ignored, never an error.
        _mediator.DispatchResult = _ => new RecordPresenceResult(false);

        var response = await _client.PostAsync(PresenceUrl, content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadAsStringAsync()).Length);
    }

    [Fact]
    public async Task Should_Return204_When_PresenceConsented()
    {
        _mediator.DispatchResult = _ => ConsentToPresenceResult.Success();

        var response = await _client.PutAsync(ConsentUrl, content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return404NotMember_When_ConsentingWithoutProfile()
    {
        _mediator.DispatchResult = _ => ConsentToPresenceResult.Failure(CommunityErrors.Community.NotMember);

        var response = await _client.PutAsync(ConsentUrl, content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertCodeAsync("not-member", response);
    }

    [Theory]
    [InlineData("""{ "code": "rain" }""", "rain")]
    [InlineData("""{ "code": null }""", null)]
    [InlineData("{}", null)]
    public async Task Should_Return204AndSendCode_When_AmbianceIsChosen(string body, string? expected)
    {
        ChooseAmbianceCommand? sent = null;
        _mediator.DispatchResult = command =>
        {
            sent = (ChooseAmbianceCommand)command;
            return ChooseAmbianceResult.Success();
        };

        var response = await PutJsonAsync(AmbianceUrl, body);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(new ChooseAmbianceCommand(expected), sent);
    }

    [Fact]
    public async Task Should_Return400UnknownAmbiance_When_CodeNotInCatalogue()
    {
        _mediator.DispatchResult = _ => ChooseAmbianceResult.Failure(CommunityErrors.Community.UnknownAmbiance);

        var response = await PutJsonAsync(AmbianceUrl, """{ "code": "nope" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertCodeAsync("unknown-ambiance", response);
    }

    [Fact]
    public async Task Should_Return404NotMember_When_ChoosingAmbianceWithoutProfile()
    {
        _mediator.DispatchResult = _ => ChooseAmbianceResult.Failure(CommunityErrors.Community.NotMember);

        var response = await PutJsonAsync(AmbianceUrl, """{ "code": "rain" }""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertCodeAsync("not-member", response);
    }

    [Fact]
    public async Task Should_ReturnAmbianceCatalogue_When_Requested()
    {
        _mediator.SendResult = _ => (IReadOnlyList<AmbianceViewModel>)
            [new AmbianceViewModel("rain", "Rain", "Nature", "https://example.test/rain")];

        var response = await _client.GetAsync(AmbiancesUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync("""
            [ { "code": "rain", "label": "Rain", "category": "Nature", "url": "https://example.test/rain" } ]
            """, response);
    }

    [Fact]
    public async Task Should_ReturnOpenSpaceContractWithoutIdentifiers_When_Requested()
    {
        _mediator.SendResult = _ => new GetOpenSpaceResult(
            OpenSpaceAccess.Open,
            ViewerInGroup: false,
            Desks:
            [
                new DeskViewModel(
                    [new MemberChipViewModel("RenardAgile", null, true), new MemberChipViewModel("LoutreTenace", null, false)],
                    SharedEnd, "Sprint", "Running", IsClosedToJoin: false,
                    new AmbianceViewModel("rain", "Rain", "Nature", "https://example.test/rain"))
            ],
            Connected: [new MemberChipViewModel("CastorPaisible", "https://avatars.githubusercontent.com/u/3", false)],
            ConnectedTotal: 1,
            ActiveToday: new ActiveTodayViewModel(1, [new MemberChipViewModel("CastorPaisible", null, false)]));

        var response = await _client.GetAsync(OpenSpaceUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        AssertNoForbiddenKey(body);
        Assert.Equal("Open", body["access"]!.GetValue<string>());
        Assert.True(body["desks"]![0]!["seated"]![0]!["isMe"]!.GetValue<bool>());
        Assert.False(body["desks"]![0]!["seated"]![1]!["isMe"]!.GetValue<bool>());
        Assert.Equal("https://example.test/rain", body["desks"]![0]!["ambiance"]!["url"]!.GetValue<string>());
        Assert.Equal(1, body["connectedTotal"]!.GetValue<int>());
    }

    [Fact]
    public async Task Should_ReturnAmbiance_When_CurrentSessionRequested()
    {
        _mediator.SendResult = _ => new GetCurrentSessionResult(
            IsGroup: true, Role: "Host", Phase: "Running", StepNumber: 1, SprintNumber: 1,
            StepType: "Sprint", StepEndsAt: SharedEnd, WaitLimitAt: null, EndReason: null,
            Others: [], IsClosedToJoin: false,
            Ambiance: new AmbianceViewModel("rain", "Rain", "Nature", "https://example.test/rain"));

        var response = await _client.GetAsync(CurrentSessionUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        AssertNoForbiddenKey(body);
        Assert.Equal("rain", body["ambiance"]!["code"]!.GetValue<string>());
        Assert.Equal("https://example.test/rain", body["ambiance"]!["url"]!.GetValue<string>());
    }

    // The open sprints list and the active-today list are replaced by the open space (#50).
    [Theory]
    [InlineData("api/community/open-sprints")]
    [InlineData("api/community/active-today")]
    public async Task Should_Return404_When_OldOpenSprintsRouteRequested(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            IsClosedToJoin: true);
        _mediator.SendResult = _ => PomodoroGetCurrentSessionResult.WithSession(session);
        _mediator.DispatchResult = _ => StartSessionResult.Success(session);

        var get = await _client.GetAsync(SessionUrl);
        var post = await _client.PostAsync(SessionUrl + "?type=Sprint", content: null);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        foreach (var response in new[] { get, post })
        {
            var body = await ReadJsonAsync(response);
            Assert.Equal("2026-10-30T14:25:00Z", body["sharedEndAt"]!.GetValue<string>());
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
        GitHubLoginAvailable: true,
        PresenceConsented: true,
        AmbianceCode: null);

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
        OpenSprint: new OpenSprintStatusViewModel(
            SharedEnd, [new SprintMemberViewModel("LoutreTenace", null)], IncludesViewer: false, StepType: "Sprint", Phase: "Running"),
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
