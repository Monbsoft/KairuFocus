using KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetFocusSummaryQueryHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly GetFocusSummaryQueryHandler _sut;

    public GetFocusSummaryQueryHandlerTests()
    {
        _sut = new GetFocusSummaryQueryHandler(
            _sessionRepository,
            _settingsRepository,
            new FakeCurrentUserService(),
            NullLogger<GetFocusSummaryQueryHandler>.Instance);
    }

    private static PomodoroSession FreeSprintCompleted(DateTime startedAt, int durationMinutes)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Complete(startedAt.AddMinutes(durationMinutes));
        return session;
    }

    private static PomodoroSession RegularSprintCompleted(DateTime startedAt, int plannedDurationMinutes, int actualDurationMinutes)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedDurationMinutes, FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Complete(startedAt.AddMinutes(actualDurationMinutes));
        return session;
    }

    [Fact]
    public async Task Should_ReturnSprintsTodayCount_When_SprintsCompletedToday()
    {
        var now = DateTime.UtcNow;
        _sessionRepository.Sessions.Add(FreeSprintCompleted(now.AddHours(-3), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(now.AddHours(-2), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(2, result.SprintsToday);
    }

    [Fact]
    public async Task Should_SumDurations_When_ComputingFocusMinutesToday()
    {
        var now = DateTime.UtcNow;
        // 25 min + 10 min => 35 minutes
        _sessionRepository.Sessions.Add(FreeSprintCompleted(now.AddHours(-3), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(now.AddHours(-1), 10));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(35, result.FocusMinutesToday);
    }

    [Fact]
    public async Task Should_SumAllCompletedSprintDurations_When_ComputingFocusMinutesToday()
    {
        var now = DateTime.UtcNow;
        // Mix of a regular sprint (25 min) and a free sprint (10 min) completed today => 35 minutes.
        // Focus minutes must cover ALL completed sprints, not just free ones.
        _sessionRepository.Sessions.Add(RegularSprintCompleted(now.AddHours(-3), plannedDurationMinutes: 25, actualDurationMinutes: 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(now.AddHours(-1), 10));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(35, result.FocusMinutesToday);
        Assert.Equal(2, result.SprintsToday);
    }

    [Fact]
    public async Task Should_ReturnDailyGoalFromSettings_When_Queried()
    {
        _settingsRepository.Settings = PomodoroSettings.Create(25, 5, 15, 6).Value;

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(6, result.DailySprintGoal);
    }

    [Fact]
    public async Task Should_ReturnStreak_When_ConsecutiveDays()
    {
        var todayStart = DateTime.UtcNow.Date.AddHours(9);
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart, 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddDays(-1), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddDays(-2), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(3, result.Streak);
    }

    [Fact]
    public async Task Should_ReturnZeroFocus_When_NoSessionsToday()
    {
        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(0, result.FocusMinutesToday);
        Assert.Equal(0, result.SprintsToday);
        Assert.Equal(0, result.Streak);
        Assert.Equal(PomodoroSettings.DefaultDailySprintGoal, result.DailySprintGoal);
    }
}
