using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using KairuFocus.Api.Tests.Auth;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;
using KairuFocus.Application.Gamification.Queries.GetCommunityPresence;
using KairuFocus.Application.Gamification.Queries.GetCommunityProgress;
using KairuFocus.Application.Gamification.Queries.GetCommunityRecap;

namespace KairuFocus.Api.Tests.Gamification;

/// <summary>
/// HTTP adapter of the community page (plan #42 §7): authentication, period parsing, error mapping
/// and the exact JSON contracts. The use cases themselves are covered by the Application tests.
/// </summary>
public sealed class CommunityEndpointsTests : IClassFixture<GamificationApiFixture>
{
    private const string ProgressUrl = "api/gamification/community/progress";
    private const string LeaderboardUrl = "api/gamification/community/leaderboard";
    private const string RecapUrl = "api/gamification/community/recap";
    private const string PresenceUrl = "api/gamification/community/presence";

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
    [InlineData("GET", ProgressUrl)]
    [InlineData("GET", LeaderboardUrl)]
    [InlineData("GET", RecapUrl)]
    [InlineData("GET", PresenceUrl)]
    public async Task Should_Return401_When_CommunityEndpointIsCalledAnonymously(string method, string url)
    {
        var response = await _anonymousClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // The generated alias is gone: the old membership endpoints no longer exist.
    // (Only GET is asserted: the other verbs answer 405 on an unknown route because of the host's fallback.)
    [Fact]
    public async Task Should_Return404_When_RemovedMembershipEndpointIsCalled()
    {
        var response = await _client.GetAsync("api/gamification/leaderboard/membership");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Period parsing ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProgressUrl + "?period=7")]
    [InlineData(ProgressUrl + "?period=%20week%20")]
    [InlineData(LeaderboardUrl + "?period=year")]
    [InlineData(RecapUrl + "?period=Week,Month")]
    public async Task Should_Return400_When_PeriodIsInvalid(string url)
    {
        var sentQueries = CaptureQueries();

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertJsonAsync("""{ "error": "Invalid period." }""", response);
        Assert.Empty(sentQueries);
    }

    [Theory]
    [InlineData(ProgressUrl)]
    [InlineData(LeaderboardUrl)]
    [InlineData(RecapUrl)]
    [InlineData(ProgressUrl + "?period=")]
    public async Task Should_DefaultToWeek_When_PeriodIsMissing(string url)
    {
        var sentQueries = CaptureQueries();

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CommunityPeriod.Week, PeriodOf(Assert.Single(sentQueries)));
    }

    [Theory]
    [InlineData(ProgressUrl + "?period=month", CommunityPeriod.Month)]
    [InlineData(LeaderboardUrl + "?period=GLOBAL", CommunityPeriod.Global)]
    [InlineData(RecapUrl + "?period=Week", CommunityPeriod.Week)]
    public async Task Should_SendParsedPeriod_When_PeriodIsValid(string url, CommunityPeriod expected)
    {
        var sentQueries = CaptureQueries();

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, PeriodOf(Assert.Single(sentQueries)));
    }

    // ── JSON contracts (plan §7) ───────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnProgressContract_When_ProgressIsRequested()
    {
        _mediator.SendResult = _ => SampleProgress;

        var response = await _client.GetAsync(ProgressUrl + "?period=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync("""
            {
              "windowStart": "2026-10-19",
              "xp": 120, "sprints": 12, "focusMinutes": 300,
              "comparison": { "previousXp": 90, "previousCutoffAt": "2026-10-16T14:00:00+00:00", "deltaXp": 30 },
              "averageWeeklyXp": null,
              "allTime": { "totalXp": 1240, "sprints": 124, "firstGainAt": "2026-07-16T09:12:00+00:00" },
              "bestWeek": { "periodStart": "2026-09-21", "xp": 180 },
              "bestMonth": { "periodStart": "2026-09-01", "xp": 520 },
              "bars": [ { "periodStart": "2026-08-31", "xp": 40, "isCurrent": false, "isRecord": false } ]
            }
            """, response);
    }

    [Fact]
    public async Task Should_ReturnLeaderboardContract_When_LeaderboardIsRequested()
    {
        _mediator.SendResult = _ => SampleLeaderboard;

        var response = await _client.GetAsync(LeaderboardUrl + "?period=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync("""
            {
              "isParticipant": true, "rankedCount": 23,
              "me": { "rank": 4, "isTied": false, "xp": 120, "name": "RenardAgile", "avatarUrl": null, "isInTop": true },
              "top": [ { "rank": 1, "name": "LoutreTenace", "avatarUrl": "https://avatars.githubusercontent.com/u/1", "xp": 210, "isMe": false } ]
            }
            """, response);
    }

    [Fact]
    public async Task Should_ReturnRecapContract_When_RecapIsRequested()
    {
        _mediator.SendResult = _ => SampleRecap;

        var response = await _client.GetAsync(RecapUrl + "?period=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync(
            """{ "sprints": 1240, "focusMinutes": 31200, "activeMembers": 48, "mySprints": 12 }""",
            response);
    }

    [Fact]
    public async Task Should_ReturnPresenceContract_When_PresenceIsRequested()
    {
        _mediator.SendResult = query => query is GetCommunityPresenceQuery
            ? SamplePresence
            : throw new InvalidOperationException($"Unexpected query {query.GetType().Name}.");

        var response = await _client.GetAsync(PresenceUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync("""{ "inSprintNow": 12, "sprintedToday": 37 }""", response);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static readonly GetCommunityProgressResult SampleProgress = new(
        WindowStart: new DateOnly(2026, 10, 19),
        Xp: 120,
        Sprints: 12,
        FocusMinutes: 300,
        Comparison: new XpComparisonViewModel(
            PreviousXp: 90,
            PreviousCutoffAt: new DateTimeOffset(2026, 10, 16, 14, 0, 0, TimeSpan.Zero),
            DeltaXp: 30),
        AverageWeeklyXp: null,
        AllTime: new AllTimeViewModel(
            TotalXp: 1240,
            Sprints: 124,
            FirstGainAt: new DateTimeOffset(2026, 7, 16, 9, 12, 0, TimeSpan.Zero)),
        BestWeek: new XpRecordViewModel(new DateOnly(2026, 9, 21), 180),
        BestMonth: new XpRecordViewModel(new DateOnly(2026, 9, 1), 520),
        Bars: [new XpBarViewModel(new DateOnly(2026, 8, 31), 40, IsCurrent: false, IsRecord: false)]);

    private static readonly GetCommunityLeaderboardResult SampleLeaderboard = new(
        IsParticipant: true,
        RankedCount: 23,
        Me: new MyStandingViewModel(Rank: 4, IsTied: false, Xp: 120, Name: "RenardAgile", AvatarUrl: null, IsInTop: true),
        Top: [new LeaderboardRowViewModel(Rank: 1, Name: "LoutreTenace", AvatarUrl: "https://avatars.githubusercontent.com/u/1", Xp: 210, IsMe: false)]);

    private static readonly GetCommunityRecapResult SampleRecap = new(
        Sprints: 1240, FocusMinutes: 31200, ActiveMembers: 48, MySprints: 12);

    private static readonly GetCommunityPresenceResult SamplePresence = new(InSprintNow: 12, SprintedToday: 37);

    /// <summary>Answers every period-based query with its sample result and records the queries sent.</summary>
    private List<object> CaptureQueries()
    {
        var sentQueries = new List<object>();
        _mediator.SendResult = query =>
        {
            sentQueries.Add(query);
            return query switch
            {
                GetCommunityProgressQuery => SampleProgress,
                GetCommunityLeaderboardQuery => SampleLeaderboard,
                GetCommunityRecapQuery => SampleRecap,
                _ => throw new InvalidOperationException($"Unexpected query {query.GetType().Name}.")
            };
        };
        return sentQueries;
    }

    private static CommunityPeriod PeriodOf(object query) => query switch
    {
        GetCommunityProgressQuery progress => progress.Period,
        GetCommunityLeaderboardQuery leaderboard => leaderboard.Period,
        GetCommunityRecapQuery recap => recap.Period,
        _ => throw new InvalidOperationException($"Query {query.GetType().Name} has no period.")
    };

    private static Task AssertErrorAsync(string expectedError, HttpResponseMessage response) =>
        AssertJsonAsync(new JsonObject { ["error"] = expectedError }.ToJsonString(), response);

    /// <summary>Exact structural comparison: same property names, values and no extra property.</summary>
    private static async Task AssertJsonAsync(string expectedJson, HttpResponseMessage response)
    {
        var actualJson = await response.Content.ReadAsStringAsync();

        Assert.True(
            JsonNode.DeepEquals(JsonNode.Parse(expectedJson), JsonNode.Parse(actualJson)),
            $"Expected JSON:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual JSON:{Environment.NewLine}{actualJson}");
    }
}
