using KairuFocus.Web.Helpers;
using KairuFocus.Web.Services;

namespace KairuFocus.Web.Tests.Helpers;

public sealed class AccountIdentityTests
{
    private static CommunityMembershipDto Membership(bool isMember) =>
        new(isMember, "tomtom", "GitHub", "https://img/a.png", "tom", true);

    [Fact]
    public void Should_UseProfileName_When_Member() =>
        Assert.Equal("tomtom", AccountIdentity.DisplayName(Membership(true), "login", "Name"));

    [Fact]
    public void Should_UseLogin_When_NotMember() =>
        Assert.Equal("login", AccountIdentity.DisplayName(Membership(false), "login", "Name"));

    [Fact]
    public void Should_UseName_When_NoLogin() =>
        Assert.Equal("Name", AccountIdentity.DisplayName(null, null, "Name"));

    [Fact]
    public void Should_ReturnQuestionMark_When_NothingKnown() =>
        Assert.Equal("?", AccountIdentity.DisplayName(null, " ", null));

    [Fact]
    public void Should_ShowAvatar_When_Member() =>
        Assert.Equal("https://img/a.png", AccountIdentity.AvatarUrl(Membership(true)));

    [Fact]
    public void Should_HideAvatar_When_NotMember() =>
        Assert.Null(AccountIdentity.AvatarUrl(Membership(false)));
}
