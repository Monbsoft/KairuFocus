namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <summary>
/// What any connected user may read of a member: no user identifier, email, display name or session
/// identifier (spec §6.4). A zone is null with its *Failed flag set when its read failed: the other zones
/// are still returned (arbitrage A1).
/// </summary>
/// <param name="JoinedMonth">First day of the month the member joined the community.</param>
/// <param name="IsMe">The target is the current user.</param>
/// <param name="ViewerIsMember">The current user has a community profile.</param>
/// <param name="OpenSprint">Also null, without failure, when the member has no open sprint.</param>
public sealed record MemberProfileViewModel(
    string Name,
    bool IsGitHub,
    string? AvatarUrl,
    DateOnly JoinedMonth,
    bool IsMe,
    bool ViewerIsMember,
    MemberStandingViewModel? Standing,
    bool StandingFailed,
    MemberActivityViewModel? Activity,
    bool ActivityFailed,
    OpenSprintStatusViewModel? OpenSprint,
    bool OpenSprintFailed);
