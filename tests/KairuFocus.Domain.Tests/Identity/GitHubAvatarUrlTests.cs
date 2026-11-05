using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Tests.Identity;

public sealed class GitHubAvatarUrlTests
{
    private const string Origin = "https://avatars.githubusercontent.com";

    public static TheoryData<string?> RejectedUrls =>
    [
        null,
        "",
        "not a url",
        "/u/1",
        "http://avatars.githubusercontent.com/u/1?v=4",
        "https://evil.example/x.png",
        "https://avatars.githubusercontent.com.evil.example/x",
        "https://avatars.githubusercontent.com@evil.example/x",
        "https://user:secret@avatars.githubusercontent.com/u/1",
        "https://avatars.githubusercontent.com:8443/u/1",
        " https://avatars.githubusercontent.com/u/1",
        "https://avatars.githubusercontent.com/u/1 x",
        "https://avatars.githubusercontent.com/u/1\n",
        Origin + "/" + new string('a', GitHubAvatarUrl.MaxLength),
    ];

    [Fact]
    public void Should_KeepUrl_When_UrlIsGitHubAvatar()
    {
        const string url = "https://avatars.githubusercontent.com/u/1?v=4";

        Assert.Equal(url, GitHubAvatarUrl.Sanitize(url));
    }

    [Fact]
    public void Should_KeepUrl_When_UrlHasExactlyMaxLength()
    {
        var url = Origin + "/" + new string('a', GitHubAvatarUrl.MaxLength - Origin.Length - 1);

        Assert.Equal(GitHubAvatarUrl.MaxLength, url.Length);
        Assert.Equal(url, GitHubAvatarUrl.Sanitize(url));
    }

    [Theory]
    [MemberData(nameof(RejectedUrls))]
    public void Should_ReturnNull_When_UrlIsNotAcceptable(string? url)
    {
        Assert.Null(GitHubAvatarUrl.Sanitize(url));
    }
}
