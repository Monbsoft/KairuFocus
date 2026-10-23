using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

public sealed class EfCoreCommunityActivityRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WeekStart = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WeekEnd = WeekStart.AddDays(7);

    private readonly InMemoryDatabase _database = new();

    // --- Presence -------------------------------------------------------------------------------

    [Fact]
    public async Task Should_CountTimedSprint_When_ItIsBeforeItsDeadline()
    {
        await _database.SeedAsync(ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 25, Now.AddMinutes(-10)));

        var inProgress = await QueryAsync(r => r.CountSprintsInProgressAsync(Now));

        Assert.Equal(1, inProgress);
    }

    // Deadline = StartedAt + PlannedDurationMinutes (ADR-025); an expired session never observed stays Active.
    [Fact]
    public async Task Should_ExcludeTimedSprint_When_ItsDeadlineHasPassed()
    {
        await _database.SeedAsync(
            ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 25, Now.AddMinutes(-25)),
            ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 25, Now.AddMinutes(-30)));

        var inProgress = await QueryAsync(r => r.CountSprintsInProgressAsync(Now));

        Assert.Equal(0, inProgress);
    }

    [Fact]
    public async Task Should_ExcludeFreeSprint_When_StartedFourHoursAgoOrMore()
    {
        await _database.SeedAsync(
            ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 0, Now.AddHours(-4)),
            ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 0, Now.AddHours(-5)),
            ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 0, Now.AddHours(-4).AddMinutes(1)));

        var inProgress = await QueryAsync(r => r.CountSprintsInProgressAsync(Now));

        Assert.Equal(1, inProgress);
    }

    [Fact]
    public async Task Should_ExcludeActiveBreaks_When_CountingSprintsInProgress()
    {
        await _database.SeedAsync(
            ActiveSession(UserId.New(), PomodoroSessionType.ShortBreak, 5, Now.AddMinutes(-1)),
            ActiveSession(UserId.New(), PomodoroSessionType.LongBreak, 15, Now.AddMinutes(-1)),
            ActiveSession(UserId.New(), PomodoroSessionType.Sprint, 25, Now.AddMinutes(-1)));

        var inProgress = await QueryAsync(r => r.CountSprintsInProgressAsync(Now));

        Assert.Equal(1, inProgress);
    }

    // --- Active members -------------------------------------------------------------------------

    [Fact]
    public async Task Should_CountDistinctOwners_When_CountingActiveMembers()
    {
        var twoSprints = UserId.New();
        var oneSprint = UserId.New();
        await _database.SeedAsync(
            CompletedSprint(twoSprints, WeekStart.AddHours(1), WeekStart.AddHours(2)),
            CompletedSprint(twoSprints, WeekStart.AddHours(3), WeekStart.AddHours(4)),
            CompletedSprint(oneSprint, WeekStart.AddHours(5), WeekStart.AddHours(6)),
            CompletedSprint(UserId.New(), WeekStart.AddDays(-2), WeekStart.AddDays(-2).AddHours(1)),
            InterruptedSprint(UserId.New(), WeekStart.AddHours(1), WeekStart.AddHours(2)),
            CompletedBreak(UserId.New(), WeekStart.AddHours(1), WeekStart.AddHours(1).AddMinutes(5)));

        var activeMembers = await QueryAsync(r => r.CountActiveMembersAsync(WeekStart, WeekEnd));

        Assert.Equal(2, activeMembers);
    }

    [Fact]
    public async Task Should_ExcludeSessionsWithoutOwner_When_AggregatingCommunity()
    {
        // Legacy rows: PomodoroSessions.OwnerId is nullable.
        await _database.SeedAsync(
            CompletedSprint(owner: null, WeekStart.AddHours(1), WeekStart.AddHours(2)),
            ActiveSession(owner: null, PomodoroSessionType.Sprint, 25, Now.AddMinutes(-1)));

        var inProgress = await QueryAsync(r => r.CountSprintsInProgressAsync(Now));
        var activeMembers = await QueryAsync(r => r.CountActiveMembersAsync(WeekStart, WeekEnd));
        var totals = await QueryAsync(r => r.GetCompletedSprintTotalsAsync(WeekStart, WeekEnd));

        Assert.Equal(0, inProgress);
        Assert.Equal(0, activeMembers);
        Assert.Equal(new CompletedSprintTotals(0, TimeSpan.Zero), totals);
    }

    // --- Completed sprint totals ----------------------------------------------------------------

    [Fact]
    public async Task Should_CapEachSprintAt4Hours_When_SummingFocus()
    {
        var owner = UserId.New();
        await _database.SeedAsync(
            CompletedSprint(owner, WeekStart.AddHours(1), WeekStart.AddHours(6)),
            CompletedSprint(owner, WeekStart.AddHours(7), WeekStart.AddHours(7).AddMinutes(25), plannedMinutes: 25));

        var totals = await QueryAsync(r => r.GetCompletedSprintTotalsAsync(WeekStart, WeekEnd));

        Assert.Equal(new CompletedSprintTotals(2, TimeSpan.FromHours(4) + TimeSpan.FromMinutes(25)), totals);
    }

    // Unlike XP, the community counts sprints without the 5-minute floor.
    [Fact]
    public async Task Should_IncludeSprintsUnder5Minutes_When_CountingCompletedSprints()
    {
        await _database.SeedAsync(CompletedSprint(UserId.New(), WeekStart.AddHours(1), WeekStart.AddHours(1).AddMinutes(2)));

        var totals = await QueryAsync(r => r.GetCompletedSprintTotalsAsync(WeekStart, WeekEnd));

        Assert.Equal(new CompletedSprintTotals(1, TimeSpan.FromMinutes(2)), totals);
    }

    [Fact]
    public async Task Should_CountOnlyCompletedSprintsEndedWithinWindow_When_SummingFocus()
    {
        var owner = UserId.New();
        await _database.SeedAsync(
            CompletedSprint(owner, WeekStart.AddMinutes(-10), WeekStart),
            CompletedSprint(owner, WeekEnd.AddMinutes(-10), WeekEnd),
            CompletedSprint(owner, WeekStart.AddMinutes(-20), WeekStart.AddTicks(-1)),
            InterruptedSprint(owner, WeekStart.AddHours(1), WeekStart.AddHours(2)),
            CompletedBreak(owner, WeekStart.AddHours(3), WeekStart.AddHours(3).AddMinutes(5)));

        var totals = await QueryAsync(r => r.GetCompletedSprintTotalsAsync(WeekStart, WeekEnd));

        Assert.Equal(new CompletedSprintTotals(1, TimeSpan.FromMinutes(10)), totals);
    }

    // --- First XP gain --------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnEarliestGainAcrossUsers_When_GettingFirstXpGain()
    {
        var earliest = WeekStart.AddDays(-40);
        await _database.SeedAsync(
            XpGain.Credit(UserId.New(), Guid.NewGuid(), 10, WeekStart.AddDays(-3)),
            XpGain.Credit(UserId.New(), Guid.NewGuid(), 10, earliest),
            XpGain.Credit(UserId.New(), Guid.NewGuid(), 10, WeekStart.AddDays(-10)));

        var firstGain = await QueryAsync(r => r.GetFirstXpGainUtcAsync());

        Assert.Equal(earliest, firstGain);
    }

    [Fact]
    public async Task Should_ReturnNull_When_LedgerIsEmpty()
    {
        var firstGain = await QueryAsync(r => r.GetFirstXpGainUtcAsync());

        Assert.Null(firstGain);
    }

    private async Task<T> QueryAsync<T>(Func<EfCoreCommunityActivityRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreCommunityActivityRepository(context));
    }

    private static PomodoroSession ActiveSession(UserId? owner, PomodoroSessionType type, int plannedMinutes, DateTime startedAt)
    {
        var session = PomodoroSession.Create(type, plannedMinutes, owner!);
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedSprint(UserId? owner, DateTime startedAt, DateTime endedAt, int plannedMinutes = 0)
    {
        var session = ActiveSession(owner, PomodoroSessionType.Sprint, plannedMinutes, startedAt);
        Assert.True(session.Complete(endedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession InterruptedSprint(UserId owner, DateTime startedAt, DateTime endedAt)
    {
        var session = ActiveSession(owner, PomodoroSessionType.Sprint, plannedMinutes: 0, startedAt);
        Assert.True(session.Interrupt(endedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedBreak(UserId owner, DateTime startedAt, DateTime endedAt)
    {
        var session = ActiveSession(owner, PomodoroSessionType.ShortBreak, plannedMinutes: 5, startedAt);
        Assert.True(session.Complete(endedAt).IsSuccess);
        return session;
    }
}
