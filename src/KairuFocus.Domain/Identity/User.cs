using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Identity;

public sealed class User : Entity<UserId>
{
    public string GitHubId { get; private set; } = default!;
    public string Login { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? AvatarUrl { get; private set; }

    // For EF Core materialization
    private User() : base() { }

    private User(UserId id, string githubId, string login, string displayName, string? email)
        : base(id)
    {
        GitHubId = githubId;
        Login = login;
        DisplayName = displayName;
        Email = email;
    }

    public static User Create(string githubId, string login, string displayName, string? email, string? avatarUrl = null)
        => new(UserId.New(), githubId, login, displayName, email)
        {
            AvatarUrl = GitHubAvatarUrl.Sanitize(avatarUrl),
        };

    /// <summary>
    /// Copies the login and the avatar received from GitHub at sign-in.
    /// Returns true when a field changed, so that the caller only saves when needed.
    /// </summary>
    public bool RefreshGitHubProfile(string login, string? avatarUrl)
    {
        var sanitizedAvatarUrl = GitHubAvatarUrl.Sanitize(avatarUrl);
        if (login == Login && sanitizedAvatarUrl == AvatarUrl)
            return false;

        Login = login;
        AvatarUrl = sanitizedAvatarUrl;
        return true;
    }
}
