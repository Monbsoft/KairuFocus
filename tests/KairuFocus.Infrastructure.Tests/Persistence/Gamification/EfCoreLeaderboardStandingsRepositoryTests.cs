using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

public sealed class EfCoreLeaderboardStandingsRepositoryTests
{
    private static readonly DateTime WeekStart = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WeekEnd = WeekStart.AddDays(7);

    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_IncludeOnlyCommunityMembers_When_ComputingStandings()
    {
        var participant = UserId.New();
        var nonParticipant = UserId.New();
        var participantWithoutGain = UserId.New();
        await _database.SeedAsync(
            Profile(participant, "LoutreTenace"),
            Profile(participantWithoutGain, "HibouCalme"),
            Gain(participant, 10, WeekStart.AddHours(1)),
            Gain(nonParticipant, 10, WeekStart.AddHours(2)));

        var standings = await QueryAsync(r => r.GetStandingsBetweenAsync(WeekStart, WeekEnd));

        Assert.Equal([Standing("LoutreTenace", 10)], standings);
    }

    [Fact]
    public async Task Should_SumOnlyGainsWithinWindow_When_PeriodIsBounded()
    {
        var participant = UserId.New();
        await _database.SeedAsync(
            Profile(participant, "LoutreTenace"),
            Gain(participant, 10, WeekStart),
            Gain(participant, 20, WeekEnd.AddTicks(-1)),
            Gain(participant, 40, WeekStart.AddTicks(-1)),
            Gain(participant, 80, WeekEnd));

        var standings = await QueryAsync(r => r.GetStandingsBetweenAsync(WeekStart, WeekEnd));

        Assert.Equal([Standing("LoutreTenace", 30)], standings);
    }

    [Fact]
    public async Task Should_SumAllGains_When_StandingsAreAllTime()
    {
        var first = UserId.New();
        var second = UserId.New();
        await _database.SeedAsync(
            Profile(first, "LoutreTenace"),
            Profile(second, "HibouCalme"),
            Gain(first, 10, WeekStart.AddYears(-1)),
            Gain(first, 20, WeekStart.AddHours(1)),
            Gain(second, 10, WeekStart.AddMonths(-2)));

        var standings = await QueryAsync(r => r.GetAllTimeStandingsAsync());

        Assert.Equal(
            [Standing("HibouCalme", 10), Standing("LoutreTenace", 30)],
            standings.OrderBy(s => s.Name.Value, StringComparer.Ordinal));
    }

    // The opt-in governs who appears, not what is counted.
    [Fact]
    public async Task Should_CountGainsEarnedBeforeJoining_When_ComputingStandings()
    {
        var participant = UserId.New();
        await _database.SeedAsync(
            Profile(participant, "LoutreTenace", joinedAtUtc: WeekStart.AddDays(3)),
            Gain(participant, 10, WeekStart.AddDays(1)));

        var periodStandings = await QueryAsync(r => r.GetStandingsBetweenAsync(WeekStart, WeekEnd));
        var allTimeStandings = await QueryAsync(r => r.GetAllTimeStandingsAsync());

        Assert.Equal([Standing("LoutreTenace", 10)], periodStandings);
        Assert.Equal([Standing("LoutreTenace", 10)], allTimeStandings);
    }

    [Fact]
    public async Task Should_ReturnAvatarOfGitHubMember_When_ComputingStandings()
    {
        const string avatar = "https://avatars.githubusercontent.com/u/1";
        var github = UserId.New();
        var chosen = UserId.New();
        await _database.SeedAsync(
            Profile(github, "LoutreTenace", source: ProfileNameSource.GitHub, avatarUrl: avatar),
            Profile(chosen, "HibouCalme"),
            Gain(github, 10, WeekStart.AddHours(1)),
            Gain(github, 5, WeekStart.AddHours(2)),
            Gain(chosen, 20, WeekStart.AddHours(3)));

        var standings = await QueryAsync(r => r.GetStandingsBetweenAsync(WeekStart, WeekEnd));

        Assert.Equal(
            [Standing("HibouCalme", 20), Standing("LoutreTenace", 15, avatar)],
            standings.OrderBy(s => s.Name.Value, StringComparer.Ordinal));
    }

    private async Task<IReadOnlyList<XpStanding>> QueryAsync(
        Func<EfCoreLeaderboardStandingsRepository, Task<IReadOnlyList<XpStanding>>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreLeaderboardStandingsRepository(context));
    }

    private static MemberProfile Profile(
        UserId userId, string name, DateTime? joinedAtUtc = null, ProfileNameSource source = ProfileNameSource.Chosen, string? avatarUrl = null) =>
        MemberProfile.Join(userId, ProfileName.Create(name).Value, source, avatarUrl, joinedAtUtc ?? WeekStart.AddDays(-30));

    private static XpGain Gain(UserId ownerId, int amount, DateTime earnedAtUtc) =>
        XpGain.Credit(ownerId, Guid.NewGuid(), amount, earnedAtUtc);

    private static XpStanding Standing(string name, int xp, string? avatarUrl = null) =>
        new(ProfileName.Create(name).Value, avatarUrl, xp);
}
