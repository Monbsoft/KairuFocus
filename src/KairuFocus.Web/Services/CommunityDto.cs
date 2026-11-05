namespace KairuFocus.Web.Services;

// ── Community profile (iteration #44) ───────────────────────────────────────
// Mirrors of the API contracts (api/community/*). The Web project does not reference the Application
// project. Calendar dates are UTC DateOnly values to display as-is; instants are DateTimeOffset values.
// No contract carries a user or session identifier.

/// <summary>GET membership. Name, NameSource ("GitHub" or "Chosen") and AvatarUrl are null for a non-member.
/// GitHubLogin is the login proposed as a name; GitHubLoginAvailable tells whether it can be taken.</summary>
public sealed record CommunityMembershipDto(
    bool IsMember,
    string? Name,
    string? NameSource,
    string? AvatarUrl,
    string? GitHubLogin,
    bool GitHubLoginAvailable);

/// <summary>A member shown in a stack or a list: name and optional avatar.</summary>
public sealed record SprintMemberDto(string Name, string? AvatarUrl);

/// <summary>GET members/{name}. A zone is null with its *Failed flag set when its read failed;
/// OpenSprint is also null, without failure, when the member has no open sprint.</summary>
public sealed record MemberProfileDto(
    string Name,
    bool IsGitHub,
    string? AvatarUrl,
    DateOnly JoinedMonth,
    bool IsMe,
    bool ViewerIsMember,
    MemberStandingDto? Standing,
    bool StandingFailed,
    MemberActivityDto? Activity,
    bool ActivityFailed,
    OpenSprintStatusDto? OpenSprint,
    bool OpenSprintFailed);

/// <summary>All-time XP and general rank (null rank: not ranked).</summary>
public sealed record MemberStandingDto(int Xp, int? Rank);

/// <summary>Days with at least one sprint over the last 371 UTC days, ascending.</summary>
public sealed record MemberActivityDto(int Sprints, int FocusMinutes, int SprintsTogether, IReadOnlyList<ActivityDayDto> Days);

public sealed record ActivityDayDto(DateOnly Date, int Sprints);

/// <summary>Others: the group without the member and without the viewer. IncludesViewer: the viewer is already in it.</summary>
public sealed record OpenSprintStatusDto(DateTimeOffset EndsAt, IReadOnlyList<SprintMemberDto> Others, bool IncludesViewer);

/// <summary>GET open-sprints: one row per group.</summary>
public sealed record OpenSprintsDto(bool ViewerIsMember, IReadOnlyList<OpenSprintRowDto> Groups);

public sealed record OpenSprintRowDto(string Name, string? AvatarUrl, DateTimeOffset EndsAt, int ParticipantCount, bool IsMine);

/// <summary>GET current-sprint/participants: the other members of my group.</summary>
public sealed record SprintParticipantsDto(IReadOnlyList<SprintMemberDto> Others);

// ── Results ─────────────────────────────────────────────────────────────────

public enum CommunityOutcome
{
    Succeeded,
    /// <summary>404: not a member (leaving then has nothing left to do).</summary>
    NotMember,
    /// <summary>Any other failure (server error, network, unreadable body).</summary>
    Failed
}

public enum MemberLookupStatus { Found, NotFound, Failed }

public sealed record MemberProfileLookup(MemberLookupStatus Status, MemberProfileDto? Profile = null);

public enum NameChangeOutcome
{
    Succeeded,
    /// <summary>400 invalid-name: the name breaks the rules.</summary>
    InvalidName,
    /// <summary>400/409 name-unavailable: reserved, taken or another user's login (the reason is never told).</summary>
    NameUnavailable,
    /// <summary>409 already-member (join only): the real state must be read again.</summary>
    AlreadyMember,
    /// <summary>404 not-member (change only).</summary>
    NotMember,
    Failed
}

/// <summary>Membership is the new state after a success.</summary>
public sealed record NameChangeResult(NameChangeOutcome Outcome, CommunityMembershipDto? Membership = null);

public enum JoinSprintOutcome
{
    Succeeded,
    /// <summary>403 not-member.</summary>
    NotMember,
    /// <summary>409 sprint-active: the visitor already has a sprint.</summary>
    SprintActive,
    /// <summary>409 own-sprint.</summary>
    OwnSprint,
    /// <summary>410 sprint-closed: finished, elapsed or closed to arrivals.</summary>
    SprintClosed,
    Failed
}

public sealed record JoinSprintResult(JoinSprintOutcome Outcome);

/// <summary>Error body of the API: <c>{ error, code }</c>.</summary>
internal sealed record ApiErrorBody(string? Error, string? Code);
