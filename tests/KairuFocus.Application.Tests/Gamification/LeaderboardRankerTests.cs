using KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class LeaderboardRankerTests
{
    private static ProfileName P(string value) => ProfileName.Create(value).Value;

    private static XpStanding S(string name, int xp, string? avatarUrl = null) => new(P(name), avatarUrl, xp);

    /// <summary>"Membre" + letters A, B, C… (valid profile names).</summary>
    private static List<XpStanding> Members(params int[] xps) =>
        xps.Select((xp, i) => S("Membre" + (char)('A' + i), xp)).ToList();

    [Fact]
    public void Should_AssignStandardCompetitionRanks_When_XpAreTied()
    {
        var standings = new[] { S("AbeilleCalme", 10), S("BisonFiable", 8), S("CastorSage", 8), S("DauphinVif", 5) };

        var result = LeaderboardRanker.Rank(standings, me: null, isParticipant: false);

        Assert.Equal([1, 2, 2, 4], result.Top.Select(r => r.Rank));
    }

    [Fact]
    public void Should_ExcludeMember_When_XpIsZero()
    {
        var standings = new[] { S("AbeilleCalme", 10), S("BisonFiable", 0) };

        var result = LeaderboardRanker.Rank(standings, me: null, isParticipant: false);

        var row = Assert.Single(result.Top);
        Assert.Equal("AbeilleCalme", row.Name);
    }

    [Fact]
    public void Should_CountOnlyMembersWithXp_When_ComputingRankedCount()
    {
        var standings = new[] { S("AbeilleCalme", 10), S("BisonFiable", 0), S("CastorSage", 5), S("DauphinVif", 0) };

        var result = LeaderboardRanker.Rank(standings, me: null, isParticipant: false);

        Assert.Equal(2, result.RankedCount);
    }

    [Fact]
    public void Should_LimitTopToTenRows_When_MoreMembersAreRanked()
    {
        var standings = Members(120, 110, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10);

        var result = LeaderboardRanker.Rank(standings, me: null, isParticipant: false);

        Assert.Equal(LeaderboardRanker.TopCount, result.Top.Count);
        Assert.Equal(12, result.RankedCount);
        Assert.Equal(Enumerable.Range(1, 10), result.Top.Select(r => r.Rank));
    }

    [Fact]
    public void Should_ExcludeTiesBeyondTenthRow_When_TopIsFull()
    {
        // 10th and 11th rows share rank 10: the top is strict, the 11th row stays out of it.
        var standings = Members(100, 90, 80, 70, 60, 50, 40, 30, 20, 10, 10);
        var me = standings[^1].Name; // "MembreK", sorted after "MembreJ" at equal XP

        var result = LeaderboardRanker.Rank(standings, me, isParticipant: true);

        Assert.Equal(10, result.Top.Count);
        Assert.DoesNotContain(result.Top, r => r.IsMe);
        Assert.Equal(new MyStandingViewModel(Rank: 10, IsTied: true, Xp: 10, Name: "MembreK", AvatarUrl: null, IsInTop: false), result.Me);
    }

    [Fact]
    public void Should_OrderTiedMembersByName_When_XpIsEqual()
    {
        // Ordinal comparison: uppercase letters sort before lowercase ones ("HibouZen" < "Hiboucalme").
        var standings = new[] { S("Hiboucalme", 8), S("HibouZen", 8), S("AbeilleCalme", 8), S("ZebreVif", 9) };

        var result = LeaderboardRanker.Rank(standings, me: null, isParticipant: false);

        Assert.Equal(["ZebreVif", "AbeilleCalme", "HibouZen", "Hiboucalme"], result.Top.Select(r => r.Name));
    }

    [Fact]
    public void Should_FlagMeInTop_When_MyRowIsInTopTen()
    {
        var standings = new[] { S("AbeilleCalme", 30), S("BisonFiable", 20), S("RenardAgile", 10) };

        var result = LeaderboardRanker.Rank(standings, P("RenardAgile"), isParticipant: true);

        Assert.Equal(new MyStandingViewModel(Rank: 3, IsTied: false, Xp: 10, Name: "RenardAgile", AvatarUrl: null, IsInTop: true), result.Me);
        var myRow = Assert.Single(result.Top, r => r.IsMe);
        Assert.Equal("RenardAgile", myRow.Name);
    }

    [Fact]
    public void Should_ReturnMyRowOutsideTop_When_IAmEleventhOrBelow()
    {
        var standings = Members(120, 110, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10);
        var me = standings[^1].Name;

        var result = LeaderboardRanker.Rank(standings, me, isParticipant: true);

        Assert.NotNull(result.Me);
        Assert.Equal(12, result.Me.Rank);
        Assert.False(result.Me.IsInTop);
        Assert.DoesNotContain(result.Top, r => r.IsMe);
    }

    [Fact]
    public void Should_FlagTie_When_AnotherMemberHasMyXp()
    {
        var standings = new[] { S("AbeilleCalme", 30), S("BisonFiable", 20), S("RenardAgile", 20), S("TigreSage", 5) };

        var result = LeaderboardRanker.Rank(standings, P("RenardAgile"), isParticipant: true);

        Assert.NotNull(result.Me);
        Assert.True(result.Me.IsTied);
        Assert.Equal(2, result.Me.Rank);
    }

    [Fact]
    public void Should_ReturnNoMe_When_IAmNotParticipant()
    {
        var standings = new[] { S("AbeilleCalme", 30), S("BisonFiable", 20) };

        var result = LeaderboardRanker.Rank(standings, me: null, isParticipant: false);

        Assert.False(result.IsParticipant);
        Assert.Null(result.Me);
        Assert.Equal(2, result.Top.Count);
        Assert.DoesNotContain(result.Top, r => r.IsMe);
    }

    [Fact]
    public void Should_ReturnNoMe_When_MyXpIsZero()
    {
        var standings = new[] { S("AbeilleCalme", 30), S("RenardAgile", 0) };

        var result = LeaderboardRanker.Rank(standings, P("RenardAgile"), isParticipant: true);

        Assert.True(result.IsParticipant);
        Assert.Null(result.Me);
        Assert.Equal(1, result.RankedCount);
    }

    [Fact]
    public void Should_ReturnEmptyTop_When_NobodyIsRanked()
    {
        var result = LeaderboardRanker.Rank([], P("RenardAgile"), isParticipant: true);

        Assert.Empty(result.Top);
        Assert.Equal(0, result.RankedCount);
        Assert.Null(result.Me);
    }

    [Fact]
    public void Should_CarryAvatarUrl_When_MemberHasOne()
    {
        const string avatar = "https://avatars.githubusercontent.com/u/1";
        var standings = new[] { S("AbeilleCalme", 30, avatar), S("RenardAgile", 20) };

        var result = LeaderboardRanker.Rank(standings, P("AbeilleCalme"), isParticipant: true);

        Assert.Equal(avatar, result.Top[0].AvatarUrl);
        Assert.Null(result.Top[1].AvatarUrl);
        Assert.Equal(avatar, result.Me?.AvatarUrl);
    }
}
