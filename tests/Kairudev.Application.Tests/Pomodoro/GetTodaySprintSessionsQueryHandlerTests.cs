using Kairudev.Application.Pomodoro.Queries.GetTodaySprintSessions;
using Kairudev.Application.Tests.Common;
using Kairudev.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairudev.Application.Tests.Pomodoro;

public sealed class GetTodaySprintSessionsQueryHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly GetTodaySprintSessionsQueryHandler _sut;

    public GetTodaySprintSessionsQueryHandlerTests()
    {
        _sut = new GetTodaySprintSessionsQueryHandler(
            _sessionRepository,
            new FakeCurrentUserService(),
            NullLogger<GetTodaySprintSessionsQueryHandler>.Instance);
    }

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
        var now = DateTime.UtcNow;
        var completed = CreateFreeSprintCompleted(now.AddHours(-2));
        var interrupted = CreateFreeSprintInterrupted(now.AddHours(-1));
        _sessionRepository.Sessions.Add(completed);
        _sessionRepository.Sessions.Add(interrupted);

        var result = await _sut.Handle(new GetTodaySprintSessionsQuery());

        Assert.Equal(2, result.Sessions.Count);
        Assert.All(result.Sessions, s => Assert.Equal(0, s.PlannedDurationMinutes));
    }

    [Fact]
    public async Task Should_NotReturnRegularSprintSessions_When_MixedSessionsExist()
    {
        var now = DateTime.UtcNow;
        var freeSprint = CreateFreeSprintCompleted(now.AddHours(-3));
        var regularSprint25 = CreateRegularSprintCompleted(25, now.AddHours(-2));
        var regularSprint50 = CreateRegularSprintCompleted(50, now.AddHours(-1));
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
        var now = DateTime.UtcNow;
        var first = CreateFreeSprintCompleted(now.AddHours(-3));
        var second = CreateFreeSprintInterrupted(now.AddHours(-2));
        var third = CreateFreeSprintCompleted(now.AddHours(-1));
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
