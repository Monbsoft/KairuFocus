using System.Net.Http.Json;
using System.Text.Json;

namespace KairuFocus.Web.Services;

public sealed class GamificationApiClient
{
    private const string CommunityRoute = "api/gamification/community";

    private readonly HttpClient _http;

    public GamificationApiClient(HttpClient http) => _http = http;

    public async Task<XpSummaryDto?> GetXpSummaryAsync()
    {
        using var response = await _http.GetAsync("api/gamification/xp");
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

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await _http.GetAsync(url, ct);
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

    /// <summary>The exceptions an API call can raise: network error, timeout, unreadable body.</summary>
    private static bool IsApiCallFailure(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException;
}
