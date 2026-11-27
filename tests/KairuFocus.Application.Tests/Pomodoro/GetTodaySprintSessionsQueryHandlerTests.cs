using KairuFocus.Application.Pomodoro.Queries.GetTodaySprintSessions;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetTodaySprintSessionsQueryHandlerTests
{
    // The handler's clock is pinned (2026-06-26 12:00 UTC), so the local day is deterministic
    // whatever the time the tests run at.
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly GetTodaySprintSessionsQueryHandler _sut;

    public GetTodaySprintSessionsQueryHandlerTests()
    {
        _sut = BuildSut(FixedNow);
    }

    private GetTodaySprintSessionsQueryHandler BuildSut(DateTimeOffset now) =>
        new(
            _sessionRepository,
            new FakeCurrentUserService(),
            NullLogger<GetTodaySprintSessionsQueryHandler>.Instance,
            new FixedTimeProvider(now));

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static DateTime TodayUtcAt(int hour) => FixedNow.UtcDateTime.Date.AddHours(hour);

    private PomodoroSession CreateFreeSprintCompleted(DateTime startedAt)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Complete(startedAt.AddMinutes(42));
        return session;
    }

    private PomodoroSession CreateFreeSprintInterrupted(DateTime startedAt)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Interrupt(startedAt.AddMinutes(15));
        return session;
    }

    private PomodoroSession CreateRegularSprintCompleted(int plannedDurationMinutes, DateTime startedAt)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedDurationMinutes, FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Complete(startedAt.AddMinutes(plannedDurationMinutes));
        return session;
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoFreeSprintsToday()
    {
        var result = await _sut.Handle(new GetTodaySprintSessionsQuery());

        Assert.Empty(result.Sessions);
    }

    [Fact]
    public async Task Should_ReturnFreeSprintSessions_When_SessionsExist()
    {
        var completed = CreateFreeSprintCompleted(TodayUtcAt(9));
        var interrupted = CreateFreeSprintInterrupted(TodayUtcAt(10));
        _sessionRepository.Sessions.Add(completed);
        _sessionRepository.Sessions.Add(interrupted);

        var result = await _sut.Handle(new GetTodaySprintSessionsQuery());

        Assert.Equal(2, result.Sessions.Count);
        Assert.All(result.Sessions, s => Assert.Equal(0, s.PlannedDurationMinutes));
    }

    [Fact]
    public async Task Should_NotReturnRegularSprintSessions_When_MixedSessionsExist()
    {
        var freeSprint = CreateFreeSprintCompleted(TodayUtcAt(9));
        var regularSprint25 = CreateRegularSprintCompleted(25, TodayUtcAt(10));
        var regularSprint50 = CreateRegularSprintCompleted(50, TodayUtcAt(11));
        _sessionRepository.Sessions.Add(freeSprint);
        _sessionRepository.Sessions.Add(regularSprint25);
        _sessionRepository.Sessions.Add(regularSprint50);

        var result = await _sut.Handle(new GetTodaySprintSessionsQuery());

        Assert.Single(result.Sessions);
        Assert.Equal(0, result.Sessions[0].PlannedDurationMinutes);
    }

    [Fact]
    public async Task Should_OrderSessionsChronologically_When_MultipleSessionsExist()
    {
        var first = CreateFreeSprintCompleted(TodayUtcAt(9));
        var second = CreateFreeSprintInterrupted(TodayUtcAt(10));
        var third = CreateFreeSprintCompleted(TodayUtcAt(11));
        // Ajout dans le désordre
        _sessionRepository.Sessions.Add(third);
        _sessionRepository.Sessions.Add(first);
        _sessionRepository.Sessions.Add(second);

        var result = await _sut.Handle(new GetTodaySprintSessionsQuery());

        Assert.Equal(3, result.Sessions.Count);
        Assert.True(result.Sessions[0].StartedAt <= result.Sessions[1].StartedAt);
        Assert.True(result.Sessions[1].StartedAt <= result.Sessions[2].StartedAt);
    }

    [Fact]
    public async Task Should_ListSprintStartedAfterLocalMidnight_When_OffsetIsPositive()
    {
        // Now = 2026-06-26 23:30 UTC = 00:30 local on the 27th (UTC+1): local day starts at 2026-06-26 23:00 UTC.
        var sut = BuildSut(new DateTimeOffset(2026, 6, 26, 23, 30, 0, TimeSpan.Zero));
        var afterLocalMidnight = CreateFreeSprintCompleted(Utc(2026, 6, 26, 23, 20)); // 00:20 local, the 27th
        var beforeLocalMidnight = CreateFreeSprintCompleted(Utc(2026, 6, 26, 22, 50)); // 23:50 local, the 26th
        _sessionRepository.Sessions.Add(afterLocalMidnight);
        _sessionRepository.Sessions.Add(beforeLocalMidnight);

        var result = await sut.Handle(new GetTodaySprintSessionsQuery(OffsetMinutes: 60));

        var listed = Assert.Single(result.Sessions);
        Assert.Equal(afterLocalMidnight.Id.Value, listed.Id);
    }

    [Fact]
    public async Task Should_ListSprintStartedBeforeUtcMidnight_When_OffsetIsNegative()
    {
        // Now = 2026-06-27 02:00 UTC = 21:00 local on the 26th (UTC-5): local day starts at 2026-06-26 05:00 UTC.
        var sut = BuildSut(new DateTimeOffset(2026, 6, 27, 2, 0, 0, TimeSpan.Zero));
        var afterUtcMidnight = CreateFreeSprintCompleted(Utc(2026, 6, 27, 1, 0)); // 20:00 local, the 26th
        var beforeUtcMidnight = CreateFreeSprintCompleted(Utc(2026, 6, 26, 23, 0)); // 18:00 local, the 26th (UTC day: the 26th)
        var previousLocalDay = CreateFreeSprintCompleted(Utc(2026, 6, 26, 4, 30)); // 23:30 local, the 25th
        _sessionRepository.Sessions.Add(afterUtcMidnight);
        _sessionRepository.Sessions.Add(previousLocalDay);
        _sessionRepository.Sessions.Add(beforeUtcMidnight);

        var result = await sut.Handle(new GetTodaySprintSessionsQuery(OffsetMinutes: -300));

        Assert.Equal(
            [beforeUtcMidnight.Id.Value, afterUtcMidnight.Id.Value],
            result.Sessions.Select(s => s.Id));
    }
}
