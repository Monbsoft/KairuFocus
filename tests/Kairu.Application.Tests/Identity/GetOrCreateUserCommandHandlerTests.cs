using Kairu.Application.Identity.Commands.GetOrCreateUser;
using Kairu.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairu.Application.Tests.Identity;

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

    [Fact]
    public async Task Should_NotCreateDuplicate_When_CalledTwice()
    {
        var command = new GetOrCreateUserCommand("gh-1", "octocat", "The Octocat", null);

        await _sut.Handle(command);
        await _sut.Handle(command);

        Assert.Single(_repository.Users);
    }
}
