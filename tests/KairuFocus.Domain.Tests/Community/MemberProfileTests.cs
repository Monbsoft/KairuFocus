using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Tests.Community;

public sealed class MemberProfileTests
{
    private const string AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4";

    private static readonly UserId User = UserId.From(new Guid("00000000-0000-0000-0000-000000000001"));
    private static readonly DateTime JoinedAtUtc = new(2026, 10, 30, 9, 30, 0, DateTimeKind.Utc);

    private static ProfileName Name(string value) => ProfileName.Create(value).Value;

    [Fact]
    public void Should_KeepAvatar_When_SourceIsGitHub()
    {
        var profile = MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc);

        Assert.Equal(User, profile.Id);
        Assert.Equal(Name("octocat"), profile.Name);
        Assert.Equal("octocat", profile.NameKey);
        Assert.Equal(ProfileNameSource.GitHub, profile.NameSource);
        Assert.Equal(AvatarUrl, profile.AvatarUrl);
        Assert.Equal(JoinedAtUtc, profile.JoinedAtUtc);
    }

    [Fact]
    public void Should_DropAvatar_When_SourceIsChosen()
    {
        var profile = MemberProfile.Join(User, Name("Lea_Dev"), ProfileNameSource.Chosen, AvatarUrl, JoinedAtUtc);

        Assert.Equal(ProfileNameSource.Chosen, profile.NameSource);
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public void Should_RemoveAvatar_When_ChangingFromGitHubToChosen()
    {
        var profile = MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc);

        profile.ChangeName(Name("Lea_Dev"), ProfileNameSource.Chosen, AvatarUrl);

        Assert.Equal(ProfileNameSource.Chosen, profile.NameSource);
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public void Should_SetAvatar_When_ChangingFromChosenToGitHub()
    {
        var profile = MemberProfile.Join(User, Name("Lea_Dev"), ProfileNameSource.Chosen, null, JoinedAtUtc);

        profile.ChangeName(Name("octocat"), ProfileNameSource.GitHub, AvatarUrl);

        Assert.Equal(ProfileNameSource.GitHub, profile.NameSource);
        Assert.Equal(AvatarUrl, profile.AvatarUrl);
    }

    [Fact]
    public void Should_UpdateNameKey_When_NameChanges()
    {
        var profile = MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc);

        profile.ChangeName(Name("Lea_Dev"), ProfileNameSource.Chosen, null);

        Assert.Equal(Name("Lea_Dev"), profile.Name);
        Assert.Equal("lea_dev", profile.NameKey);
    }

    [Fact]
    public void Should_KeepJoinDate_When_NameChanges()
    {
        var profile = MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc);

        profile.ChangeName(Name("Lea_Dev"), ProfileNameSource.Chosen, null);

        Assert.Equal(JoinedAtUtc, profile.JoinedAtUtc);
    }

    [Fact]
    public void Should_Throw_When_JoiningWithoutJoinDate()
    {
        Assert.Throws<ArgumentException>(() =>
            MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, default));
    }

    [Fact]
    public void Should_Throw_When_JoiningWithoutName()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MemberProfile.Join(User, null!, ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc));
    }

    [Fact]
    public void Should_Throw_When_ChangingToNullName()
    {
        var profile = MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc);

        Assert.Throws<ArgumentNullException>(() =>
            profile.ChangeName(null!, ProfileNameSource.Chosen, null));
    }

    private static readonly AmbianceCatalogue Catalogue = new(
    [
        new Ambiance("lofi", "Lofi", AmbianceCategory.Lofi, AmbianceCatalogue.YouTubeVideoPrefix + "abc123")
    ]);

    private static MemberProfile NewProfile() =>
        MemberProfile.Join(User, Name("octocat"), ProfileNameSource.GitHub, AvatarUrl, JoinedAtUtc);

    [Fact]
    public void Should_ConsentToPresence_When_Joining()
    {
        var profile = NewProfile();

        Assert.True(profile.HasConsentedToPresence);
        Assert.Equal(JoinedAtUtc, profile.PresenceConsentedAtUtc);
    }

    [Fact]
    public void Should_KeepFirstConsent_When_ConsentingTwice()
    {
        var profile = NewProfile();

        profile.ConsentToPresence(JoinedAtUtc.AddDays(1));

        Assert.Equal(JoinedAtUtc, profile.PresenceConsentedAtUtc);
    }

    [Fact]
    public void Should_SetAmbiance_When_CodeIsInCatalogue()
    {
        var profile = NewProfile();

        var result = profile.ChooseAmbiance("lofi", Catalogue);

        Assert.True(result.IsSuccess);
        Assert.Equal("lofi", profile.AmbianceCode);
    }

    [Fact]
    public void Should_ClearAmbiance_When_CodeIsNull()
    {
        var profile = NewProfile();
        profile.ChooseAmbiance("lofi", Catalogue);

        var result = profile.ChooseAmbiance(null, Catalogue);

        Assert.True(result.IsSuccess);
        Assert.Null(profile.AmbianceCode);
    }

    [Fact]
    public void Should_RefuseAmbiance_When_CodeIsUnknown()
    {
        var profile = NewProfile();
        profile.ChooseAmbiance("lofi", Catalogue);

        var result = profile.ChooseAmbiance("nope", Catalogue);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Community.UnknownAmbiance, result.Error);
        Assert.Equal("lofi", profile.AmbianceCode);
    }
}
