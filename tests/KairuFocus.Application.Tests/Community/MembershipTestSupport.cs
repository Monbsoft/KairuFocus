using KairuFocus.Application.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Community;

/// <summary>Current user whose id is the one of a <see cref="User"/> stored in a fake user repository.</summary>
internal sealed class MembershipCurrentUser : ICurrentUserService
{
    public MembershipCurrentUser(UserId userId) => CurrentUserId = userId;

    public UserId CurrentUserId { get; }
}

internal static class MembershipTestData
{
    public const string AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4";
    public static readonly DateTime NowUtc = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);
}
