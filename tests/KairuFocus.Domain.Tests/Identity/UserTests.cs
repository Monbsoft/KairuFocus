using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Tests.Identity;

public sealed class UserTests
{
    [Fact]
    public void Should_CreateUser_When_ValidDataProvided()
    {
        var user = User.Create("12345", "octocat", "The Octocat", "octocat@github.com");

        Assert.Equal("12345", user.GitHubId);
        Assert.Equal("octocat", user.Login);
        Assert.Equal("The Octocat", user.DisplayName);
        Assert.Equal("octocat@github.com", user.Email);
    }

    [Fact]
    public void Should_GenerateGuidId_When_Created()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.NotEqual(Guid.Empty, user.Id.Value);
        Assert.NotEqual(user.GitHubId, user.Id.Value.ToString());
    }

    [Fact]
    public void Should_AllowNullEmail_When_Created()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.Null(user.Email);
    }

    // ── Avatar and GitHub profile ──────────────────────────────────────────

    private const string AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4";

    [Fact]
    public void Should_HaveNoAvatar_When_CreatedWithoutAvatar()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.Null(user.AvatarUrl);
    }

    [Fact]
    public void Should_KeepAvatar_When_CreatedWithGitHubAvatar()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null, AvatarUrl);

        Assert.Equal(AvatarUrl, user.AvatarUrl);
    }

    [Fact]
    public void Should_DropAvatar_When_CreatedWithForeignAvatar()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null, "https://evil.example/x.png");

        Assert.Null(user.AvatarUrl);
    }

    [Fact]
    public void Should_ReturnFalse_When_ProfileIsUnchanged()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null, AvatarUrl);

        var changed = user.RefreshGitHubProfile("octocat", AvatarUrl);

        Assert.False(changed);
        Assert.Equal("octocat", user.Login);
        Assert.Equal(AvatarUrl, user.AvatarUrl);
    }

    [Fact]
    public void Should_UpdateLoginAndAvatar_When_ProfileChanged()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null, AvatarUrl);
        const string newAvatarUrl = "https://avatars.githubusercontent.com/u/2?v=4";

        var changed = user.RefreshGitHubProfile("hubot", newAvatarUrl);

        Assert.True(changed);
        Assert.Equal("hubot", user.Login);
        Assert.Equal(newAvatarUrl, user.AvatarUrl);
    }

    [Fact]
    public void Should_ReturnTrue_When_OnlyLoginChanged()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null, AvatarUrl);

        Assert.True(user.RefreshGitHubProfile("hubot", AvatarUrl));
    }

    [Fact]
    public void Should_ReturnTrue_When_OnlyAvatarChanged()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.True(user.RefreshGitHubProfile("octocat", AvatarUrl));
        Assert.Equal(AvatarUrl, user.AvatarUrl);
    }

    [Fact]
    public void Should_RemoveAvatarAndReturnTrue_When_RefreshedWithForeignAvatar()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null, AvatarUrl);

        var changed = user.RefreshGitHubProfile("octocat", "https://evil.example/x.png");

        Assert.True(changed);
        Assert.Null(user.AvatarUrl);
    }

    [Fact]
    public void Should_ReturnFalse_When_ForeignAvatarRefreshedOnUserWithoutAvatar()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.False(user.RefreshGitHubProfile("octocat", "https://evil.example/x.png"));
    }

    [Fact]
    public void Should_KeepDisplayNameAndEmail_When_ProfileChanged()
    {
        var user = User.Create("12345", "octocat", "The Octocat", "octocat@github.com");

        user.RefreshGitHubProfile("hubot", AvatarUrl);

        Assert.Equal("The Octocat", user.DisplayName);
        Assert.Equal("octocat@github.com", user.Email);
    }
}
