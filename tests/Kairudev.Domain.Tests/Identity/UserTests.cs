using Kairudev.Domain.Identity;

namespace Kairudev.Domain.Tests.Identity;

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
    public void Should_SetIdFromGitHubId_When_Created()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.Equal(UserId.From("12345"), user.Id);
    }

    [Fact]
    public void Should_AllowNullEmail_When_Created()
    {
        var user = User.Create("12345", "octocat", "The Octocat", null);

        Assert.Null(user.Email);
    }
}
