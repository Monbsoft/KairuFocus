using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Pomodoro;

public sealed class EfCorePomodoroSessionRepositoryTests
{
    private static readonly DateTime Start = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();
    private readonly UserId _user = UserId.New();

    // --- GetLatestEnded -------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnNull_When_UserHasNoEndedSession()
    {
        var active = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, _user);
        Assert.True(active.Start(Start).IsSuccess);
        await _database.SeedAsync(active, Ended(UserId.New(), PomodoroSessionType.Sprint, Start, completed: true));

        var latest = await QueryAsync(r => r.GetLatestEndedAsync(_user));

        Assert.Null(latest);
    }

    [Fact]
    public async Task Should_ReturnTheOneThatEndedLast_When_UserHasSeveralEndedSessions()
    {
        var first = Ended(_user, PomodoroSessionType.Sprint, Start, completed: true);
        var last = Ended(_user, PomodoroSessionType.Sprint, Start.AddHours(2), completed: true);
        var middle = Ended(_user, PomodoroSessionType.Sprint, Start.AddHours(1), completed: true);
        await _database.SeedAsync(first, last, middle);

        var latest = await QueryAsync(r => r.GetLatestEndedAsync(_user));

        Assert.Equal(last.Id, latest!.Id);
    }

    [Theory]
    [InlineData(PomodoroSessionType.Sprint, true)]
    [InlineData(PomodoroSessionType.Sprint, false)]
    [InlineData(PomodoroSessionType.ShortBreak, true)]
    [InlineData(PomodoroSessionType.ShortBreak, false)]
    [InlineData(PomodoroSessionType.LongBreak, true)]
    public async Task Should_ReturnLatest_When_ItIsOfAnyTypeAndStatus(PomodoroSessionType type, bool completed)
    {
        var earlier = Ended(_user, PomodoroSessionType.Sprint, Start, completed: true);
        var later = Ended(_user, type, Start.AddHours(1), completed);
        await _database.SeedAsync(earlier, later);

        var latest = await QueryAsync(r => r.GetLatestEndedAsync(_user));

        Assert.Equal(later.Id, latest!.Id);
        Assert.Equal(type, latest.SessionType);
        Assert.Equal(completed ? PomodoroSessionStatus.Completed : PomodoroSessionStatus.Interrupted, latest.Status);
    }

    [Fact]
    public async Task Should_IgnoreOtherUsersAndActiveSessions_When_ReadingLatestEnded()
    {
        var own = Ended(_user, PomodoroSessionType.Sprint, Start, completed: true);
        var othersLater = Ended(UserId.New(), PomodoroSessionType.Sprint, Start.AddHours(3), completed: true);
        var activeLater = PomodoroSession.Create(PomodoroSessionType.ShortBreak, 5, _user);
        Assert.True(activeLater.Start(Start.AddHours(4)).IsSuccess);
        await _database.SeedAsync(own, othersLater, activeLater);

        var latest = await QueryAsync(r => r.GetLatestEndedAsync(_user));

        Assert.Equal(own.Id, latest!.Id);
    }

    private async Task<T> QueryAsync<T>(Func<EfCorePomodoroSessionRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCorePomodoroSessionRepository(context));
    }

    private static PomodoroSession Ended(UserId owner, PomodoroSessionType type, DateTime startedAt, bool completed)
    {
        var session = PomodoroSession.Create(type, 25, owner);
        Assert.True(session.Start(startedAt).IsSuccess);
        var endedAt = startedAt.AddMinutes(25);
        Assert.True((completed ? session.Complete(endedAt) : session.Interrupt(endedAt)).IsSuccess);
        return session;
    }
}
