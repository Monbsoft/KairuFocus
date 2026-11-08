using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

/// <summary>A member of a group session (the host is not a member). The user stays in a private field:
/// reads must not project it (ADR-026).</summary>
public sealed class GroupMember
{
    private UserId _userId = default!;

    // Required by EF Core.
    private GroupMember()
    {
    }

    internal GroupMember(UserId userId, DateTime joinedAtUtc)
    {
        _userId = userId;
        JoinedAtUtc = joinedAtUtc;
    }

    public DateTime JoinedAtUtc { get; private set; }
    public DateTime? LeftAtUtc { get; private set; }
    public bool IsActive => LeftAtUtc is null;

    internal bool IsUser(UserId userId) => _userId == userId;

    internal void Leave(DateTime nowUtc) => LeftAtUtc = nowUtc;

    internal void Return(DateTime nowUtc)
    {
        JoinedAtUtc = nowUtc;
        LeftAtUtc = null;
    }
}
