using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// Create / move forward / ignore-older paths of <see cref="EfCoreMemberPresenceRepository"/>, and a presence deleted
/// during the signal (the member left the community). The race of two first signals of the same member is NOT covered
/// here: under InMemory a duplicate key surfaces as a raw ArgumentException, never as the SQL Server duplicate-key error
/// the repository recovers from, so the recovery path cannot be reproduced honestly. Nor is the insert whose profile is
/// gone (foreign key violation): InMemory enforces no foreign key. Both are checked at acceptance on SQL Server.
/// </summary>
public sealed class MemberPresenceRepositoryTests
{
    private static readonly DateTime FirstSeenAtUtc = new(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_AddPresence_When_FirstSeen()
    {
        var userId = UserId.New();

        await RecordAsync(userId, FirstSeenAtUtc);

        Assert.Equal(FirstSeenAtUtc, await LastSeenAsync(userId));
    }

    [Fact]
    public async Task Should_UpdatePresence_When_SeenAgain()
    {
        var userId = UserId.New();
        await RecordAsync(userId, FirstSeenAtUtc);

        await RecordAsync(userId, FirstSeenAtUtc.AddMinutes(2));

        Assert.Equal(FirstSeenAtUtc.AddMinutes(2), await LastSeenAsync(userId));
        Assert.Equal(1, await CountAsync());
    }

    [Fact]
    public async Task Should_KeepLatest_When_OlderSeenArrivesLate()
    {
        var userId = UserId.New();
        await RecordAsync(userId, FirstSeenAtUtc.AddMinutes(2));

        await RecordAsync(userId, FirstSeenAtUtc);

        Assert.Equal(FirstSeenAtUtc.AddMinutes(2), await LastSeenAsync(userId));
    }

    // The member left the community between the read of the presence and its update: the cascade deleted the row.
    [Fact]
    public async Task Should_IgnoreSignal_When_PresenceDeletedDuringSignal()
    {
        var userId = UserId.New();
        await RecordAsync(userId, FirstSeenAtUtc);
        await using var context = _database.CreateContext();
        Assert.NotNull(await context.MemberPresences.FindAsync(userId));
        await using (var other = _database.CreateContext())
        {
            other.MemberPresences.Remove((await other.MemberPresences.FindAsync(userId))!);
            await other.SaveChangesAsync();
        }

        var repository = new EfCoreMemberPresenceRepository(context);
        await repository.RecordSeenAsync(userId, FirstSeenAtUtc.AddMinutes(1));

        Assert.Equal(0, await CountAsync());
        // The scoped context stays usable: nothing rejected is left tracked.
        Assert.Empty(context.ChangeTracker.Entries());
        await context.SaveChangesAsync();
    }

    private async Task RecordAsync(UserId userId, DateTime seenAtUtc)
    {
        await using var context = _database.CreateContext();
        await new EfCoreMemberPresenceRepository(context).RecordSeenAsync(userId, seenAtUtc);
    }

    private async Task<DateTime?> LastSeenAsync(UserId userId)
    {
        await using var context = _database.CreateContext();
        return (await context.MemberPresences.FindAsync(userId))?.LastSeenAtUtc;
    }

    private async Task<int> CountAsync()
    {
        await using var context = _database.CreateContext();
        return context.MemberPresences.Count();
    }
}
