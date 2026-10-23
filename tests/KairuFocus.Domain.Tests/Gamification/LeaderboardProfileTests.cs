using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Tests.Gamification;

public sealed class LeaderboardProfileTests
{
    private static readonly UserId User = UserId.From(new Guid("00000000-0000-0000-0000-000000000001"));
    private static readonly DateTime JoinedAtUtc = new(2026, 10, 23, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_KeepUserPseudonymAndJoinDate_When_UserJoins()
    {
        var pseudonym = Pseudonym.Create("LoutreTenace").Value;

        var profile = LeaderboardProfile.Join(User, pseudonym, JoinedAtUtc);

        Assert.Equal(User, profile.Id);
        Assert.Equal(pseudonym, profile.Pseudonym);
        Assert.Equal(JoinedAtUtc, profile.JoinedAtUtc);
    }

    [Fact]
    public void Should_ReplacePseudonym_When_PseudonymIsChanged()
    {
        var profile = LeaderboardProfile.Join(User, Pseudonym.Create("LoutreTenace").Value, JoinedAtUtc);
        var newPseudonym = Pseudonym.Create("HibouAttentif").Value;

        profile.ChangePseudonym(newPseudonym);

        Assert.Equal(newPseudonym, profile.Pseudonym);
        Assert.Equal(User, profile.Id);
        Assert.Equal(JoinedAtUtc, profile.JoinedAtUtc);
    }

    [Fact]
    public void Should_Throw_When_JoinDateIsDefault()
    {
        var pseudonym = Pseudonym.Create("LoutreTenace").Value;

        Assert.Throws<ArgumentException>(
            () => LeaderboardProfile.Join(User, pseudonym, default));
    }
}
