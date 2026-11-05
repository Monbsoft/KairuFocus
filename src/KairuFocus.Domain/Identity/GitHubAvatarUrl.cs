namespace KairuFocus.Domain.Identity;

/// <summary>
/// Avatar URLs come from GitHub claims and are later shown to other users: only an https URL on the
/// GitHub avatar host goes through, anything else is dropped.
/// </summary>
public static class GitHubAvatarUrl
{
    public const int MaxLength = 500;

    private const string AllowedHost = "avatars.githubusercontent.com";

    /// <summary>Returns <paramref name="url"/> unchanged when it is acceptable, otherwise null.</summary>
    public static string? Sanitize(string? url)
    {
        if (string.IsNullOrEmpty(url) || url.Length > MaxLength)
            return null;

        // Uri.TryCreate trims surrounding spaces and encodes inner ones: the raw text must not contain any.
        if (url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var isAllowed = uri.Scheme == Uri.UriSchemeHttps
                        && uri.IsDefaultPort
                        && uri.UserInfo.Length == 0
                        && string.Equals(uri.Host, AllowedHost, StringComparison.OrdinalIgnoreCase);

        return isAllowed ? url : null;
    }
}
