using KairuFocus.Application.Community.Commands.JoinCommunity;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Identity;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class JoinCommunityCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly User _me = User.Create("gh-1", "octocat", "The Octocat", "octo@example.com", MembershipTestData.AvatarUrl);
    private readonly JoinCommunityCommandHandler _sut;

    public JoinCommunityCommandHandlerTests()
    {
        _users.Users.Add(_me);
        _sut = CreateSut(_users, _me.Id);
    }

    private JoinCommunityCommandHandler CreateSut(FakeUserRepository users, UserId currentUserId) =>
        new(
            _profiles,
            users,
            new MembershipCurrentUser(currentUserId),
            new FixedTimeProvider(new DateTimeOffset(MembershipTestData.NowUtc)),
            NullLogger<JoinCommunityCommandHandler>.Instance);

    [Fact]
    public async Task Should_CreateProfileWithLoginAndAvatar_When_SourceIsGitHub()
    {
        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.GitHub, null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal(_me.Id, stored.UserId);
        Assert.Equal("octocat", stored.Name.Value);
        Assert.Equal(ProfileNameSource.GitHub, stored.NameSource);
        Assert.Equal(MembershipTestData.AvatarUrl, stored.AvatarUrl);
        Assert.Equal(MembershipTestData.NowUtc, stored.JoinedAtUtc);

        Assert.NotNull(result.Membership);
        Assert.True(result.Membership.IsMember);
        Assert.Equal("octocat", result.Membership.Name);
        Assert.Equal("GitHub", result.Membership.NameSource);
        Assert.Equal(MembershipTestData.AvatarUrl, result.Membership.AvatarUrl);
        Assert.Equal("octocat", result.Membership.GitHubLogin);
        Assert.True(result.Membership.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_CreateProfileWithoutAvatar_When_SourceIsChosen()
    {
        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "lea_dev"));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal("lea_dev", stored.Name.Value);
        Assert.Equal(ProfileNameSource.Chosen, stored.NameSource);
        Assert.Null(stored.AvatarUrl);
        Assert.Null(result.Membership!.AvatarUrl);
        Assert.Equal("Chosen", result.Membership.NameSource);
    }

    [Fact]
    public async Task Should_CreateProfileWithoutAvatar_When_GitHubUserHasNoAvatar()
    {
        var withoutAvatar = User.Create("gh-2", "noavatar", "No Avatar", null);
        var users = new FakeUserRepository();
        users.Users.Add(withoutAvatar);
        var sut = CreateSut(users, withoutAvatar.Id);

        var result = await sut.Handle(new JoinCommunityCommand(ProfileNameSource.GitHub, null));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal("noavatar", stored.Name.Value);
        Assert.Equal(ProfileNameSource.GitHub, stored.NameSource);
        Assert.Null(stored.AvatarUrl);
    }

    [Fact]
    public async Task Should_TrimName_When_ChosenNameHasSurroundingSpaces()
    {
        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, " lea "));

        Assert.True(result.IsSuccess);
        Assert.Equal("lea", Assert.Single(_profiles.Stored).Name.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("lea dev")]
    [InlineData("léa")]
    public async Task Should_ReturnInvalidName_When_ChosenNameIsInvalid(string? name)
    {
        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, name));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.InvalidName, result.Error);
        Assert.Null(result.Membership);
        Assert.Empty(_profiles.Stored);
    }

    [Fact]
    public async Task Should_ReturnReservedName_When_ChosenNameIsReserved()
    {
        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "Admin"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.ReservedName, result.Error);
        Assert.Empty(_profiles.Stored);
    }

    [Fact]
    public async Task Should_ReturnNameTaken_When_NameDiffersOnlyByCase()
    {
        _profiles.Seed(UserId.New(), "lea");

        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "LEA"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NameTaken, result.Error);
        Assert.Single(_profiles.Stored);
        Assert.Equal(0, _profiles.TryAddCallCount);
    }

    [Fact]
    public async Task Should_ReturnNameTaken_When_GitHubLoginIsTaken()
    {
        _profiles.Seed(UserId.New(), "Octocat");

        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.GitHub, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NameTaken, result.Error);
    }

    [Fact]
    public async Task Should_ReturnLoginOfAnotherUser_When_ChosenNameIsLoginOfAnotherUser()
    {
        _users.Users.Add(User.Create("gh-9", "SomeoneElse", "Someone Else", null));

        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "someoneelse"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.LoginOfAnotherUser, result.Error);
        Assert.Empty(_profiles.Stored);
    }

    [Fact]
    public async Task Should_AcceptOwnLogin_When_SourceIsChosen()
    {
        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "octocat"));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_profiles.Stored);
        Assert.Equal(ProfileNameSource.Chosen, stored.NameSource);
        Assert.Null(stored.AvatarUrl);
    }

    [Fact]
    public async Task Should_ReturnAlreadyMember_When_ProfileExists()
    {
        _profiles.Seed(_me.Id, "existing");

        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "other_name"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.AlreadyMember, result.Error);
        Assert.Equal("existing", Assert.Single(_profiles.Stored).Name.Value);
        Assert.Equal(0, _profiles.TryAddCallCount);
    }

    [Fact]
    public async Task Should_FailWithInvalidName_When_GitHubLoginIsTooShort()
    {
        var shortLogin = User.Create("gh-3", "ab", "Ab", null, MembershipTestData.AvatarUrl);
        var users = new FakeUserRepository();
        users.Users.Add(shortLogin);
        var sut = CreateSut(users, shortLogin.Id);

        var result = await sut.Handle(new JoinCommunityCommand(ProfileNameSource.GitHub, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.InvalidName, result.Error);
        Assert.Empty(_profiles.Stored);
    }

    [Fact]
    public async Task Should_ReturnUserNotFound_When_UserDoesNotExist()
    {
        var sut = CreateSut(new FakeUserRepository(), _me.Id);

        var result = await sut.Handle(new JoinCommunityCommand(ProfileNameSource.GitHub, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Should_ReturnAlreadyMember_When_InsertRejectedAndProfileExists()
    {
        // Another tab of the same user wins the race between the read and the insert.
        _profiles.OnTryAdd = _ => _profiles.Seed(_me.Id, "winner");

        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "loser_name"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.AlreadyMember, result.Error);
        Assert.Equal("winner", Assert.Single(_profiles.Stored).Name.Value);
    }

    [Fact]
    public async Task Should_ReturnNameTaken_When_InsertRejectedAndNoProfile()
    {
        _profiles.RejectTryAdd = true;

        var result = await _sut.Handle(new JoinCommunityCommand(ProfileNameSource.Chosen, "lea_dev"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NameTaken, result.Error);
        Assert.Equal(1, _profiles.TryAddCallCount);
        Assert.Empty(_profiles.Stored);
    }
}
