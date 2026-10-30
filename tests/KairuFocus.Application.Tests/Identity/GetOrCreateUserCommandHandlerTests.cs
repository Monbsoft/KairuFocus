using KairuFocus.Application.Identity.Commands.GetOrCreateUser;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Identity;

public sealed class GetOrCreateUserCommandHandlerTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly GetOrCreateUserCommandHandler _sut;

    public GetOrCreateUserCommandHandlerTests()
        => _sut = new GetOrCreateUserCommandHandler(_repository, NullLogger<GetOrCreateUserCommandHandler>.Instance);

    [Fact]
    public async Task Should_CreateUser_When_UserDoesNotExist()
    {
        var command = new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", "cat@github.com");

        var result = await _sut.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Users);
        Assert.Equal("octocat", result.Value!.Login);
    }

    [Fact]
    public async Task Should_ReturnExistingUser_When_UserAlreadyExists()
    {
        var existing = User.Create("gh-1", "octocat", "The Octocat", null);
        _repository.Users.Add(existing);

        var command = new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", null);

        var result = await _sut.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Users); // pas de doublon
        Assert.Equal(existing.Id, result.Value!.UserId);
    }

    [Fact]
    public async Task Should_ReturnCorrectDisplayName_When_Created()
    {
        var command = new GetOrCreateUserCommand("gh-2", "dev", "Dev User", null);

        var result = await _sut.Handle(command);

        Assert.Equal("Dev User", result.Value!.DisplayName);
    }

    private const string Avatar = "https://avatars.githubusercontent.com/u/1?v=4";
    private const string OtherAvatar = "https://avatars.githubusercontent.com/u/2?v=4";

    [Fact]
    public async Task Should_SaveAvatar_When_UserIsNew()
    {
        var command = new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", null, Avatar);

        await _sut.Handle(command);

        Assert.Equal(Avatar, Assert.Single(_repository.Users).AvatarUrl);
    }

    [Fact]
    public async Task Should_RefreshLoginAndAvatar_When_ExistingUserChanged()
    {
        var existing = User.Create("gh-1", "octocat", "The Octocat", null, Avatar);
        _repository.Users.Add(existing);

        var result = await _sut.Handle(new GetOrCreateUserCommand("gh-1", "renamed", "The Octocat", null, OtherAvatar));

        Assert.Equal("renamed", existing.Login);
        Assert.Equal(OtherAvatar, existing.AvatarUrl);
        Assert.Equal("renamed", result.Value!.Login);
        Assert.Equal(1, _repository.UpdateCount);
    }

    [Fact]
    public async Task Should_NotWrite_When_ProfileIsUnchanged()
    {
        _repository.Users.Add(User.Create("gh-1", "octocat", "The Octocat", null, Avatar));

        await _sut.Handle(new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", null, Avatar));

        Assert.Equal(0, _repository.UpdateCount);
    }

    [Fact]
    public async Task Should_ClearAvatar_When_UrlIsNotGitHubs()
    {
        var existing = User.Create("gh-1", "octocat", "The Octocat", null, Avatar);
        _repository.Users.Add(existing);

        await _sut.Handle(new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", null, "https://evil.example/a.png"));

        Assert.Null(existing.AvatarUrl);
        Assert.Equal(1, _repository.UpdateCount);
    }

    [Fact]
    public async Task Should_KeepLogin_When_LoginClaimIsMissing()
    {
        var existing = User.Create("gh-1", "octocat", "The Octocat", null, Avatar);
        _repository.Users.Add(existing);

        var result = await _sut.Handle(new GetOrCreateUserCommand(
            "gh-1", "The Octocat", "The Octocat", null, OtherAvatar, LoginIsFromGitHub: false));

        Assert.Equal("octocat", existing.Login);
        Assert.Equal("octocat", result.Value!.Login);
        Assert.Equal(OtherAvatar, existing.AvatarUrl);
        Assert.Equal(1, _repository.UpdateCount);
    }

    [Fact]
    public async Task Should_NotCreateDuplicate_When_CalledTwice()
    {
        var command = new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", null);

        await _sut.Handle(command);
        await _sut.Handle(command);

        Assert.Single(_repository.Users);
    }
}
