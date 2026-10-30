using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Community.Queries.GetCommunityMembership;

/// <summary>
/// Community membership of the current user, shown in Settings. Only the user's own data: never a
/// <c>UserId</c>. <see cref="GitHubLogin"/> is the login proposed as a profile name and
/// <see cref="GitHubLoginAvailable"/> tells whether it can be taken (valid, and not used by another profile).
/// </summary>
public sealed record MembershipViewModel(
    bool IsMember,
    string? Name,
    string? NameSource,
    string? AvatarUrl,
    string? GitHubLogin,
    bool GitHubLoginAvailable)
{
    internal static MembershipViewModel NotMember(string? gitHubLogin, bool gitHubLoginAvailable) =>
        new(IsMember: false, Name: null, NameSource: null, AvatarUrl: null, gitHubLogin, gitHubLoginAvailable);

    internal static MembershipViewModel ForMember(MemberProfile profile, string? gitHubLogin, bool gitHubLoginAvailable) =>
        new(
            IsMember: true,
            profile.Name.Value,
            profile.NameSource.ToString(),
            profile.AvatarUrl,
            gitHubLogin,
            gitHubLoginAvailable);
}
