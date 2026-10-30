using KairuFocus.Application.Community.Commands.ChangeProfileName;
using KairuFocus.Application.Tests.Identity;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class ChangeProfileNameCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly User _me = User.Create("gh-1", "octocat", "The Octocat", null, MembershipTestData.AvatarUrl);
    private readonly ChangeProfileNameCommandHandler _sut;

    public ChangeProfileNameCommandHandlerTests()
    {
        _users.Users.Add(_me);
        _sut = new ChangeProfileNameCommandHandler(
            _profiles,
            _users,
            new MembershipCurrentUser(_me.Id),
            NullLogger<ChangeProfileNameCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_ChangeNameAndAvatar_When_SwitchingToGitHub()
    {
        _profiles.Seed(_me.Id, "bobby");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.GitHub, null));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal("octocat", stored.Name.Value);
        Assert.Equal(ProfileNameSource.GitHub, stored.NameSource);
        Assert.Equal(MembershipTestData.AvatarUrl, stored.AvatarUrl);
        Assert.Equal("octocat", result.Membership!.Name);
        Assert.Equal("GitHub", result.Membership.NameSource);
    }

    [Fact]
    public async Task Should_RemoveAvatar_When_SwitchingToChosen()
    {
        _profiles.Seed(_me.Id, "octocat", ProfileNameSource.GitHub, MembershipTestData.AvatarUrl);

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, " lea_dev "));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal("lea_dev", stored.Name.Value);
        Assert.Equal(ProfileNameSource.Chosen, stored.NameSource);
        Assert.Null(stored.AvatarUrl);
        Assert.Null(result.Membership!.AvatarUrl);
    }

    [Fact]
    public async Task Should_SucceedWithoutWrite_When_NameAndSourceAreIdentical()
    {
        _profiles.Seed(_me.Id, "bobby");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "bobby"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("bobby", result.Membership!.Name);
        Assert.Equal(0, _profiles.TryUpdateCallCount);
        // The own name is never checked (the GitHub login, proposed in the view model, is another name).
        Assert.DoesNotContain(_profiles.CheckedNames, n => n.Key == "bobby");
    }

    [Fact]
    public async Task Should_AllowCaseOnlyChange_When_KeyIsOwn()
    {
        _profiles.Seed(_me.Id, "Bob");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "bob"));

        Assert.True(result.IsSuccess);
        Assert.Equal("bob", Assert.Single(_profiles.Stored).Name.Value);
        Assert.Equal(1, _profiles.TryUpdateCallCount);
        Assert.DoesNotContain(_profiles.CheckedNames, n => n.Key == "bob");
    }

    [Fact]
    public async Task Should_AllowOwnLogin_When_SourceIsChosen()
    {
        _profiles.Seed(_me.Id, "bobby");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "octocat"));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal("octocat", stored.Name.Value);
        Assert.Equal(ProfileNameSource.Chosen, stored.NameSource);
        Assert.Null(stored.AvatarUrl);
    }

    [Fact]
    public async Task Should_ReturnNameTaken_When_NameBelongsToAnotherMember()
    {
        _profiles.Seed(_me.Id, "bobby");
        _profiles.Seed(UserId.New(), "lea");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "Lea"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NameTaken, result.Error);
        Assert.Null(result.Membership);
        Assert.Equal(0, _profiles.TryUpdateCallCount);
        Assert.Equal("bobby", _profiles.Stored.Single(p => p.UserId == _me.Id).Name.Value);
    }

    [Fact]
    public async Task Should_ReturnLoginOfAnotherUser_When_ChosenNameIsLoginOfAnotherUser()
    {
        _profiles.Seed(_me.Id, "bobby");
        _users.Users.Add(User.Create("gh-9", "someoneelse", "Someone Else", null));

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "SomeoneElse"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.LoginOfAnotherUser, result.Error);
        Assert.Equal(0, _profiles.TryUpdateCallCount);
    }

    [Fact]
    public async Task Should_ReturnInvalidName_When_ChosenNameIsInvalid()
    {
        _profiles.Seed(_me.Id, "bobby");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "no"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.InvalidName, result.Error);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_NoProfile()
    {
        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "lea_dev"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Equal(0, _profiles.TryUpdateCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_ProfileDeletedConcurrently()
    {
        _profiles.Seed(_me.Id, "bobby");
        _profiles.OnTryUpdate = _ => _profiles.Delete(_me.Id);

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "lea_dev"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
    }

    [Fact]
    public async Task Should_ReturnNameTaken_When_NameTakenConcurrently()
    {
        _profiles.Seed(_me.Id, "bobby");
        _profiles.OnTryUpdate = _ => _profiles.Seed(UserId.New(), "lea_dev");

        var result = await _sut.Handle(new ChangeProfileNameCommand(ProfileNameSource.Chosen, "lea_dev"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NameTaken, result.Error);
        Assert.Equal("bobby", _profiles.Stored.Single(p => p.UserId == _me.Id).Name.Value);
    }
}
