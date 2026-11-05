namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <param name="Found">False for a malformed or reserved name, an unknown name and a non-member alike.</param>
/// <param name="Profile">Null when <paramref name="Found"/> is false.</param>
public sealed record GetMemberProfileResult(bool Found, MemberProfileViewModel? Profile);
