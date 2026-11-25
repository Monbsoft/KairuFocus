using KairuFocus.Web.Services;

namespace KairuFocus.Web.Helpers;

public static class AccountIdentity
{
    public static string DisplayName(CommunityMembershipDto? m, string? login, string? name)
    {
        if (m is { IsMember: true } && !string.IsNullOrWhiteSpace(m.Name)) return m.Name;
        if (!string.IsNullOrWhiteSpace(login)) return login;
        if (!string.IsNullOrWhiteSpace(name)) return name;
        return "?";
    }

    public static string? AvatarUrl(CommunityMembershipDto? m) => m is { IsMember: true } ? m.AvatarUrl : null;
}
