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
    public async Task Should_IncludeOnlyOptedInUsers_When_ComputingStandings()
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
            standings.OrderBy(s => s.Pseudonym.Value, StringComparer.Ordinal));
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

    private async Task<IReadOnlyList<XpStanding>> QueryAsync(
        Func<EfCoreLeaderboardStandingsRepository, Task<IReadOnlyList<XpStanding>>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreLeaderboardStandingsRepository(context));
    }

    private static LeaderboardProfile Profile(UserId userId, string pseudonym, DateTime? joinedAtUtc = null) =>
        LeaderboardProfile.Join(userId, Pseudonym.Create(pseudonym).Value, joinedAtUtc ?? WeekStart.AddDays(-30));

    private static XpGain Gain(UserId ownerId, int amount, DateTime earnedAtUtc) =>
        XpGain.Credit(ownerId, Guid.NewGuid(), amount, earnedAtUtc);

    private static XpStanding Standing(string pseudonym, int xp) => new(Pseudonym.Create(pseudonym).Value, xp);
}
