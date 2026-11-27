using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Pomodoro;

public sealed class EfCorePomodoroSessionRepositoryTests
{
    private static readonly DateTime Start = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);

    // A local-day window expressed in UTC (here simply the UTC day of 2026-10-30).
    private static readonly DateTime WindowStart = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WindowEnd = WindowStart.AddDays(1);

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

    // --- GetLatestCompletedToday (window) -------------------------------------------------------

    [Fact]
    public async Task Should_IncludeSessionEndedAtWindowStart_When_ReadingLatestCompleted()
    {
        var atStart = EndedAt(_user, PomodoroSessionType.Sprint, WindowStart, completed: true);
        await _database.SeedAsync(atStart);

        var latest = await QueryAsync(r => r.GetLatestCompletedTodayAsync(_user, WindowStart, WindowEnd));

        Assert.Equal(atStart.Id, latest!.Id);
    }

    [Fact]
    public async Task Should_ReturnNull_When_SessionsEndedBeforeWindowStartOrAtWindowEnd()
    {
        var justBefore = EndedAt(_user, PomodoroSessionType.Sprint, WindowStart.AddMinutes(-1), completed: true);
        var atEnd = EndedAt(_user, PomodoroSessionType.Sprint, WindowEnd, completed: true);
        await _database.SeedAsync(justBefore, atEnd);

        var latest = await QueryAsync(r => r.GetLatestCompletedTodayAsync(_user, WindowStart, WindowEnd));

        Assert.Null(latest);
    }

    [Fact]
    public async Task Should_ReturnMostRecentCompletedOfAnyType_When_ReadingLatestCompleted()
    {
        var sprint = EndedAt(_user, PomodoroSessionType.Sprint, WindowStart.AddHours(10), completed: true);
        var shortBreak = EndedAt(_user, PomodoroSessionType.ShortBreak, WindowStart.AddHours(11), completed: true);
        var outsideWindow = EndedAt(_user, PomodoroSessionType.Sprint, WindowEnd.AddHours(1), completed: true);
        await _database.SeedAsync(shortBreak, sprint, outsideWindow);

        var latest = await QueryAsync(r => r.GetLatestCompletedTodayAsync(_user, WindowStart, WindowEnd));

        Assert.Equal(shortBreak.Id, latest!.Id);
    }

    [Fact]
    public async Task Should_IgnoreOtherUsersInterruptedAndActiveSessions_When_ReadingLatestCompleted()
    {
        var own = EndedAt(_user, PomodoroSessionType.Sprint, WindowStart.AddHours(9), completed: true);
        var othersLater = EndedAt(UserId.New(), PomodoroSessionType.Sprint, WindowStart.AddHours(12), completed: true);
        var interruptedLater = EndedAt(_user, PomodoroSessionType.Sprint, WindowStart.AddHours(13), completed: false);
        var activeLater = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, _user);
        Assert.True(activeLater.Start(WindowStart.AddHours(14)).IsSuccess);
        await _database.SeedAsync(own, othersLater, interruptedLater, activeLater);

        var latest = await QueryAsync(r => r.GetLatestCompletedTodayAsync(_user, WindowStart, WindowEnd));

        Assert.Equal(own.Id, latest!.Id);
    }

    // --- GetTodaySprintSessions (window) --------------------------------------------------------

    [Fact]
    public async Task Should_KeepSprintsStartedInWindowOnly_When_ReadingTodaySprints()
    {
        var beforeStart = FreeSprint(_user, WindowStart.AddMinutes(-1), completed: true);
        var atStart = FreeSprint(_user, WindowStart, completed: true);
        var lastMinute = FreeSprint(_user, WindowEnd.AddMinutes(-1), completed: true);
        var atEnd = FreeSprint(_user, WindowEnd, completed: true);
        // Started the day before and ended inside the window: the criterion is the start time.
        var startedYesterdayEndedToday = FreeSprint(_user, WindowStart.AddMinutes(-10), completed: true, lasting: TimeSpan.FromMinutes(20));
        await _database.SeedAsync(beforeStart, atStart, lastMinute, atEnd, startedYesterdayEndedToday);

        var sprints = await QueryAsync(r => r.GetTodaySprintSessionsAsync(_user, WindowStart, WindowEnd));

        Assert.Equal([atStart.Id, lastMinute.Id], sprints.Select(s => s.Id));
    }

    [Fact]
    public async Task Should_ReturnCompletedAndInterruptedFreeSprintsOrderedByStart_When_ReadingTodaySprints()
    {
        var late = FreeSprint(_user, WindowStart.AddHours(15), completed: true);
        var early = FreeSprint(_user, WindowStart.AddHours(8), completed: false);
        var middle = FreeSprint(_user, WindowStart.AddHours(11), completed: true);
        await _database.SeedAsync(late, early, middle);

        var sprints = await QueryAsync(r => r.GetTodaySprintSessionsAsync(_user, WindowStart, WindowEnd));

        Assert.Equal([early.Id, middle.Id, late.Id], sprints.Select(s => s.Id));
    }

    [Fact]
    public async Task Should_IgnoreTimedSprintsOtherTypesOtherUsersAndActiveSessions_When_ReadingTodaySprints()
    {
        var free = FreeSprint(_user, WindowStart.AddHours(9), completed: true);
        var timedSprint = EndedAt(_user, PomodoroSessionType.Sprint, WindowStart.AddHours(10), completed: true);
        var shortBreak = EndedAt(_user, PomodoroSessionType.ShortBreak, WindowStart.AddHours(11), completed: true);
        var others = FreeSprint(UserId.New(), WindowStart.AddHours(12), completed: true);
        var active = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, _user);
        Assert.True(active.Start(WindowStart.AddHours(13)).IsSuccess);
        await _database.SeedAsync(free, timedSprint, shortBreak, others, active);

        var sprints = await QueryAsync(r => r.GetTodaySprintSessionsAsync(_user, WindowStart, WindowEnd));

        Assert.Equal([free.Id], sprints.Select(s => s.Id));
    }

    private async Task<T> QueryAsync<T>(Func<EfCorePomodoroSessionRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCorePomodoroSessionRepository(context));
    }

    /// <summary>A 25-minute session that ended exactly at <paramref name="endedAt"/>.</summary>
    private static PomodoroSession EndedAt(UserId owner, PomodoroSessionType type, DateTime endedAt, bool completed) =>
        Ended(owner, type, endedAt.AddMinutes(-25), completed);

    /// <summary>A free sprint (no planned duration) started at <paramref name="startedAt"/>.</summary>
    private static PomodoroSession FreeSprint(UserId owner, DateTime startedAt, bool completed, TimeSpan? lasting = null)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, owner);
        Assert.True(session.Start(startedAt).IsSuccess);
        var endedAt = startedAt + (lasting ?? TimeSpan.FromMinutes(10));
        Assert.True((completed ? session.Complete(endedAt) : session.Interrupt(endedAt)).IsSuccess);
        return session;
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
