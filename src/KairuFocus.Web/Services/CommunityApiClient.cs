using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KairuFocus.Web.Services;

/// <summary>Client of <c>api/community</c>: membership, member profiles, open sprints.</summary>
public sealed class CommunityApiClient
{
    private const string Route = "api/community";

    private readonly HttpClient _http;

    public CommunityApiClient(HttpClient http) => _http = http;

    // ── Reads: null on any failure ──────────────────────────────────────────
    // A cancellation requested through the token is rethrown so that the caller can tell
    // "superseded" from "failed".

    public Task<CommunityMembershipDto?> GetMembershipAsync(CancellationToken ct = default) =>
        GetOrNullAsync<CommunityMembershipDto>($"{Route}/membership", ct);

    public Task<OpenSprintsDto?> GetOpenSprintsAsync(CancellationToken ct = default) =>
        GetOrNullAsync<OpenSprintsDto>($"{Route}/open-sprints", ct);

    public Task<ActiveTodayDto?> GetActiveTodayAsync(CancellationToken ct = default) =>
        GetOrNullAsync<ActiveTodayDto>($"{Route}/active-today", ct);

    /// <summary>Found, NotFound (unknown name and non-member alike: same 404) or Failed.</summary>
    public async Task<MemberProfileLookup> GetMemberAsync(string name, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"{Route}/members/{Uri.EscapeDataString(name)}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new MemberProfileLookup(MemberLookupStatus.NotFound);
            if (!response.IsSuccessStatusCode)
                return new MemberProfileLookup(MemberLookupStatus.Failed);

            var profile = await response.Content.ReadFromJsonAsync<MemberProfileDto>(ct);
            return profile is null
                ? new MemberProfileLookup(MemberLookupStatus.Failed)
                : new MemberProfileLookup(MemberLookupStatus.Found, profile);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return new MemberProfileLookup(MemberLookupStatus.Failed);
        }
    }

    // ── Membership ──────────────────────────────────────────────────────────

    /// <summary>201 → Succeeded (with the membership). source: "GitHub" or "Chosen"; name only for "Chosen".</summary>
    public Task<NameChangeResult> JoinAsync(string source, string? name, CancellationToken ct = default) =>
        SendNameAsync(HttpMethod.Post, $"{Route}/membership", HttpStatusCode.Created, source, name, ct);

    /// <summary>200 → Succeeded (with the membership).</summary>
    public Task<NameChangeResult> ChangeNameAsync(string source, string? name, CancellationToken ct = default) =>
        SendNameAsync(HttpMethod.Put, $"{Route}/membership/name", HttpStatusCode.OK, source, name, ct);

    /// <summary>204 → Succeeded, 404 → NotMember.</summary>
    public async Task<CommunityOutcome> LeaveAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.DeleteAsync($"{Route}/membership", ct);
            return response.StatusCode switch
            {
                HttpStatusCode.NoContent => CommunityOutcome.Succeeded,
                HttpStatusCode.NotFound => CommunityOutcome.NotMember,
                _ => CommunityOutcome.Failed
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return CommunityOutcome.Failed;
        }
    }

    // ── Open sprints ────────────────────────────────────────────────────────

    /// <summary>201 → Succeeded; the typed failures come from the status and the <c>code</c> of the body.</summary>
    public async Task<JoinSprintResult> JoinSprintAsync(string name, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsync($"{Route}/open-sprints/{Uri.EscapeDataString(name)}/join", content: null, ct);
            if (response.StatusCode == HttpStatusCode.Created)
                return new JoinSprintResult(JoinSprintOutcome.Succeeded);

            var code = await ReadCodeAsync(response, ct);
            return new JoinSprintResult(code switch
            {
                "not-member" => JoinSprintOutcome.NotMember,
                "sprint-active" => JoinSprintOutcome.SprintActive,
                "own-sprint" => JoinSprintOutcome.OwnSprint,
                "sprint-closed" => JoinSprintOutcome.SprintClosed,
                "waiting-for-host" => JoinSprintOutcome.WaitingForHost,
                _ => JoinSprintOutcome.Failed
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return new JoinSprintResult(JoinSprintOutcome.Failed);
        }
    }

    /// <summary>Closes or reopens my group to new arrivals. True on 204.</summary>
    public async Task<bool> SetSprintClosedAsync(bool closed, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PutAsJsonAsync($"{Route}/current-sprint/closed", new { closed }, ct);
            return response.StatusCode == HttpStatusCode.NoContent;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return false;
        }
    }

    // ── Host-driven group session (#46) ─────────────────────────────────────

    /// <summary>200 → (session, true); 204 → (null, true): no session; any failure → (null, false).</summary>
    public async Task<CurrentGroupSessionLookup> GetCurrentSessionAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"{Route}/current-session", ct);
            if (response.StatusCode == HttpStatusCode.NoContent) return new CurrentGroupSessionLookup(null, true);
            if (!response.IsSuccessStatusCode) return new CurrentGroupSessionLookup(null, false);

            var session = await response.Content.ReadFromJsonAsync<CurrentGroupSessionDto>(ct);
            return new CurrentGroupSessionLookup(session, session is not null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return new CurrentGroupSessionLookup(null, false);
        }
    }

    /// <summary>Host only. stepType: "Sprint", "ShortBreak" or "LongBreak". 201 Launched, 204 HostAlone, 409 StepRunning,
    /// 410 GroupEnded, anything else Failed.</summary>
    public async Task<LaunchStepOutcome> LaunchStepAsync(string stepType, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync($"{Route}/current-session/steps", new { type = stepType }, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.Created => LaunchStepOutcome.Launched,
                HttpStatusCode.NoContent => LaunchStepOutcome.HostAlone,
                HttpStatusCode.Conflict => LaunchStepOutcome.StepRunning,
                HttpStatusCode.Gone => LaunchStepOutcome.GroupEnded,
                _ => LaunchStepOutcome.Failed
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return LaunchStepOutcome.Failed;
        }
    }

    /// <summary>Participant: asks the server to follow the host's step. 200 → the outcome read by name; otherwise Failed.</summary>
    public async Task<FollowGroupResult> FollowAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsync($"{Route}/current-session/follow", content: null, ct);
            if (response.StatusCode != HttpStatusCode.OK) return new FollowGroupResult(FollowOutcome.Failed, null, 0);

            var body = await response.Content.ReadFromJsonAsync<FollowBody>(ct);
            return body is not null && Enum.TryParse<FollowOutcome>(body.Outcome, ignoreCase: true, out var outcome)
                && outcome != FollowOutcome.Failed
                ? new FollowGroupResult(outcome, body.EndReason, body.XpAwarded)
                : new FollowGroupResult(FollowOutcome.Failed, null, 0);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return new FollowGroupResult(FollowOutcome.Failed, null, 0);
        }
    }

    /// <summary>The host stops the session for everyone; a participant leaves it. True on 204.</summary>
    public async Task<bool> LeaveOrStopAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.DeleteAsync($"{Route}/current-session", ct);
            return response.StatusCode == HttpStatusCode.NoContent;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return false;
        }
    }

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

    private async Task<NameChangeResult> SendNameAsync(
        HttpMethod method, string url, HttpStatusCode successStatus, string source, string? name, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(new { source, name }) };
            using var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == successStatus)
            {
                var membership = await response.Content.ReadFromJsonAsync<CommunityMembershipDto>(ct);
                return membership is null
                    ? new NameChangeResult(NameChangeOutcome.Failed)
                    : new NameChangeResult(NameChangeOutcome.Succeeded, membership);
            }

            var code = await ReadCodeAsync(response, ct);
            return new NameChangeResult(code switch
            {
                "invalid-name" => NameChangeOutcome.InvalidName,
                "name-unavailable" => NameChangeOutcome.NameUnavailable,
                "already-member" => NameChangeOutcome.AlreadyMember,
                "not-member" => NameChangeOutcome.NotMember,
                _ => NameChangeOutcome.Failed
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsApiCallFailure(ex))
        {
            return new NameChangeResult(NameChangeOutcome.Failed);
        }
    }

    /// <summary>The <c>code</c> of an error body; null when the body is missing or unreadable (generic failure).</summary>
    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>(ct);
            return body?.Code;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>The exceptions an API call can raise: network error, timeout, unreadable body.</summary>
    private static bool IsApiCallFailure(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException;
}
