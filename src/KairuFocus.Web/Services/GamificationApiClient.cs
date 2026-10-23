using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KairuFocus.Web.Services;

public sealed class GamificationApiClient
{
    private const string CommunityRoute = "api/gamification/community";
    private const string MembershipRoute = "api/gamification/leaderboard/membership";

    private readonly HttpClient _http;

    public GamificationApiClient(HttpClient http) => _http = http;

    public async Task<XpSummaryDto?> GetXpSummaryAsync()
    {
        var response = await _http.GetAsync("api/gamification/xp");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<XpSummaryDto>();
    }

    // ── Community reads: null on any failure ────────────────────────────────
    // A cancellation requested through the token is rethrown so that the caller can tell
    // "superseded" from "failed".

    public Task<CommunityProgressDto?> GetCommunityProgressAsync(CommunityPeriod period, CancellationToken ct = default) =>
        GetOrNullAsync<CommunityProgressDto>($"{CommunityRoute}/progress?period={period.ToApiValue()}", ct);

    public Task<CommunityLeaderboardDto?> GetCommunityLeaderboardAsync(CommunityPeriod period, CancellationToken ct = default) =>
        GetOrNullAsync<CommunityLeaderboardDto>($"{CommunityRoute}/leaderboard?period={period.ToApiValue()}", ct);

    public Task<CommunityRecapDto?> GetCommunityRecapAsync(CommunityPeriod period, CancellationToken ct = default) =>
        GetOrNullAsync<CommunityRecapDto>($"{CommunityRoute}/recap?period={period.ToApiValue()}", ct);

    public Task<CommunityPresenceDto?> GetCommunityPresenceAsync(CancellationToken ct = default) =>
        GetOrNullAsync<CommunityPresenceDto>($"{CommunityRoute}/presence", ct);

    // ── Leaderboard membership ──────────────────────────────────────────────

    public Task<LeaderboardMembershipDto?> GetLeaderboardMembershipAsync(CancellationToken ct = default) =>
        GetOrNullAsync<LeaderboardMembershipDto>(MembershipRoute, ct);

    /// <summary>201 → Succeeded (with the pseudonym), 409 → Conflict, 503 → Unavailable.</summary>
    public Task<MembershipResult> JoinLeaderboardAsync(CancellationToken ct = default) =>
        SendMembershipAsync(HttpMethod.Post, MembershipRoute, HttpStatusCode.Created, readPseudonym: true, ct);

    /// <summary>200 → Succeeded (with the new pseudonym), 404 → NotFound, 503 → Unavailable.</summary>
    public Task<MembershipResult> RerollPseudonymAsync(CancellationToken ct = default) =>
        SendMembershipAsync(HttpMethod.Post, $"{MembershipRoute}/reroll", HttpStatusCode.OK, readPseudonym: true, ct);

    /// <summary>204 → Succeeded, 404 → NotFound.</summary>
    public Task<MembershipResult> LeaveLeaderboardAsync(CancellationToken ct = default) =>
        SendMembershipAsync(HttpMethod.Delete, MembershipRoute, HttpStatusCode.NoContent, readPseudonym: false, ct);

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return null;
        }
    }

    private async Task<MembershipResult> SendMembershipAsync(
        HttpMethod method, string url, HttpStatusCode successStatus, bool readPseudonym, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            using var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == successStatus)
            {
                if (!readPseudonym) return new MembershipResult(MembershipOutcome.Succeeded);

                var body = await response.Content.ReadFromJsonAsync<PseudonymDto>(ct);
                return string.IsNullOrEmpty(body?.Pseudonym)
                    ? new MembershipResult(MembershipOutcome.Failed)
                    : new MembershipResult(MembershipOutcome.Succeeded, body.Pseudonym);
            }

            return new MembershipResult(response.StatusCode switch
            {
                HttpStatusCode.Conflict => MembershipOutcome.Conflict,
                HttpStatusCode.NotFound => MembershipOutcome.NotFound,
                HttpStatusCode.ServiceUnavailable => MembershipOutcome.Unavailable,
                _ => MembershipOutcome.Failed
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return new MembershipResult(MembershipOutcome.Failed);
        }
    }

    /// <summary>The exceptions an API call can raise: network error, timeout, unreadable body.</summary>
    private static bool IsApiCallFailure(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException;
}
