using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

public sealed class EfCoreXpGainRepositoryTests
{
    private static readonly DateTime Day = new(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_ReturnOnlyUserGainsInChronologicalOrder_When_GettingEarnings()
    {
        var user = UserId.New();
        await _database.SeedAsync(
            XpGain.Credit(user, Guid.NewGuid(), 30, Day.AddHours(3)),
            XpGain.Credit(user, Guid.NewGuid(), 10, Day.AddHours(1)),
            XpGain.Credit(UserId.New(), Guid.NewGuid(), 99, Day),
            XpGain.Credit(user, Guid.NewGuid(), 20, Day.AddHours(2)));

        await using var context = _database.CreateContext();
        var earnings = await new EfCoreXpGainRepository(context).GetEarningsAsync(user);

        Assert.Equal(
            [
                new XpEarning(Day.AddHours(1), 10),
                new XpEarning(Day.AddHours(2), 20),
                new XpEarning(Day.AddHours(3), 30),
            ],
            earnings);
    }
}
