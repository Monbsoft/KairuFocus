using KairuFocus.Application.Community.Queries.GetCommunityMembership;
using KairuFocus.Application.Tests.Identity;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetCommunityMembershipQueryHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeMemberProfileRepository _profiles = new();

    private GetCommunityMembershipQueryHandler CreateSut(User user)
    {
        _users.Users.Add(user);
        return new GetCommunityMembershipQueryHandler(_profiles, _users, new MembershipCurrentUser(user.Id));
    }

    [Fact]
    public async Task Should_ReturnMemberState_When_Member()
    {
        var me = User.Create("gh-1", "octocat", "The Octocat", null, MembershipTestData.AvatarUrl);
        var sut = CreateSut(me);
        _profiles.Seed(me.Id, "octocat", ProfileNameSource.GitHub, MembershipTestData.AvatarUrl);

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.True(result.IsMember);
        Assert.Equal("octocat", result.Name);
        Assert.Equal("GitHub", result.NameSource);
        Assert.Equal(MembershipTestData.AvatarUrl, result.AvatarUrl);
        Assert.Equal("octocat", result.GitHubLogin);
        // The login is the member's own name: it does not count as taken.
        Assert.True(result.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_ReturnChosenNameWithoutAvatar_When_NameIsChosen()
    {
        var me = User.Create("gh-1", "octocat", "The Octocat", null, MembershipTestData.AvatarUrl);
        var sut = CreateSut(me);
        _profiles.Seed(me.Id, "bobby");

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.True(result.IsMember);
        Assert.Equal("bobby", result.Name);
        Assert.Equal("Chosen", result.NameSource);
        Assert.Null(result.AvatarUrl);
        Assert.True(result.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_ProposeLogin_When_LoginIsUsable()
    {
        var me = User.Create("gh-1", "octocat", "The Octocat", null, MembershipTestData.AvatarUrl);
        var sut = CreateSut(me);

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.False(result.IsMember);
        Assert.Null(result.Name);
        Assert.Null(result.NameSource);
        Assert.Null(result.AvatarUrl);
        Assert.Equal("octocat", result.GitHubLogin);
        Assert.True(result.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_FlagLoginUnavailable_When_LoginIsTooShort()
    {
        var sut = CreateSut(User.Create("gh-1", "ab", "Ab", null));

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.False(result.IsMember);
        Assert.Equal("ab", result.GitHubLogin);
        Assert.False(result.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_FlagLoginUnavailable_When_LoginIsTakenByAnotherMember()
    {
        var sut = CreateSut(User.Create("gh-1", "octocat", "The Octocat", null));
        _profiles.Seed(UserId.New(), "OCTOCAT");

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.False(result.IsMember);
        Assert.False(result.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_FlagLoginUnavailable_When_LoginIsReserved()
    {
        var sut = CreateSut(User.Create("gh-1", "admin", "Admin", null));

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.False(result.GitHubLoginAvailable);
    }

    [Fact]
    public async Task Should_ReturnNotMemberWithoutLogin_When_UserDoesNotExist()
    {
        var sut = new GetCommunityMembershipQueryHandler(
            _profiles, _users, new MembershipCurrentUser(UserId.New()));

        var result = await sut.Handle(new GetCommunityMembershipQuery());

        Assert.False(result.IsMember);
        Assert.Null(result.GitHubLogin);
        Assert.False(result.GitHubLoginAvailable);
    }
}
