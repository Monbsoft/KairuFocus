using KairuFocus.Application.Pomodoro.Queries.GetTodaySprintSessions;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetTodaySprintSessionsQueryHandlerTests
{
    // The fake repository, like the EF one, keeps the sessions started on its current UTC day.
    // Its clock is pinned so that the setup and the query see the same day, even near midnight UTC.
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePomodoroSessionRepository _sessionRepository = new() { TimeProvider = new FixedTimeProvider(FixedNow) };
    private readonly GetTodaySprintSessionsQueryHandler _sut;

    public GetTodaySprintSessionsQueryHandlerTests()
    {
        _sut = new GetTodaySprintSessionsQueryHandler(
            _sessionRepository,
            new FakeCurrentUserService(),
            NullLogger<GetTodaySprintSessionsQueryHandler>.Instance);
    }

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
}
