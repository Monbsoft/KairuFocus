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
        var todayStart = DateTime.UtcNow.Date;
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(9), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(10), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(2, result.SprintsToday);
    }

    [Fact]
    public async Task Should_SumDurations_When_ComputingFocusMinutesToday()
    {
        var todayStart = DateTime.UtcNow.Date;
        // 25 min + 10 min => 35 minutes
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(9), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(10), 10));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(35, result.FocusMinutesToday);
    }

    [Fact]
    public async Task Should_SumAllCompletedSprintDurations_When_ComputingFocusMinutesToday()
    {
        var todayStart = DateTime.UtcNow.Date;
        // Mix of a regular sprint (25 min) and a free sprint (10 min) completed today => 35 minutes.
        // Focus minutes must cover ALL completed sprints, not just free ones.
        _sessionRepository.Sessions.Add(RegularSprintCompleted(todayStart.AddHours(9), plannedDurationMinutes: 25, actualDurationMinutes: 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(10), 10));

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

    // ── Offset / local-day tests ───────────────────────────────────────────

    [Fact]
    public async Task Should_NotCountSprint_When_SprintEndedAtUtcNightButBelongsToTomorrowLocal()
    {
        // A sprint that ends at UTC 23:30 today falls into the NEXT local day for UTC+2 (offset=+120).
        // With OffsetMinutes=120: local time is 23:30+02:00 = 01:30 next day.
        // "Today local" starts at UTC 22:00 (midnight UTC+2) and ends at UTC 23:00 (next midnight UTC+2).
        // The sprint ends at UTC 23:30 which is AFTER endUtc = 23:00, so it must NOT be counted.

        // Anchor: use a fixed UTC midnight so that today's local window is deterministic.
        var utcMidnightToday = DateTime.UtcNow.Date;                  // 00:00 UTC today
        var sprintEndedAt = utcMidnightToday.AddHours(23).AddMinutes(30); // 23:30 UTC today

        // With offset=+120, local midnight today = UTC 22:00 (today), local midnight tomorrow = UTC 22:00+24h
        // Actually: startUtc = nowLocal.Date - offset
        //           nowLocal  = UtcNow + offset  (within the same UTC day context)
        // We just need the sprint at 23:30 UTC to fall outside [startUtc, endUtc).
        // For UTC+2: startUtc = local_midnight - 2h = UTC 22:00 (yesterday shifted), let's be explicit.
        // The test verifies behaviour, not arithmetic — we seed one sprint at 23:30 UTC today
        // and assert SprintsToday=0 with offset=120 only if 23:30 UTC > endUtc for that offset.
        // endUtc = (todayLocal+1day midnight) in UTC = startUtc + 24h.
        // startUtc = (utcNow + 120min).Date - 120min.
        // If UtcNow is within 00:00-22:00 UTC today, todayLocal.Date is today, startUtc=22:00 yesterday UTC,
        // endUtc=22:00 today UTC. Sprint at 23:30 UTC today > 22:00 UTC today => NEXT local day.

        // Guard: this test is only deterministic when UtcNow.Hour < 22.
        // If run between 22:00-00:00 UTC, skip to avoid flakiness.
        if (DateTime.UtcNow.Hour >= 22)
            return;

        _sessionRepository.Sessions.Add(FreeSprintCompleted(sprintEndedAt.AddMinutes(-25), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery(OffsetMinutes: 120));

        Assert.Equal(0, result.SprintsToday);
        Assert.Equal(0, result.FocusMinutesToday);
    }

    [Fact]
    public async Task Should_CountSprint_When_SprintEndsBeforeLocalMidnight_WithPositiveOffset()
    {
        // A sprint ending at UTC 21:00 with offset=+120 (UTC+2) ends at 23:00 local,
        // which is still "today" local. It must be counted.

        // Guard: only reliable when UtcNow.Hour < 21.
        if (DateTime.UtcNow.Hour >= 21)
            return;

        var utcMidnightToday = DateTime.UtcNow.Date;
        var sprintEndedAt = utcMidnightToday.AddHours(21); // 21:00 UTC = 23:00 UTC+2 (still today local)

        _sessionRepository.Sessions.Add(FreeSprintCompleted(sprintEndedAt.AddMinutes(-25), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery(OffsetMinutes: 120));

        Assert.Equal(1, result.SprintsToday);
    }

    [Fact]
    public async Task Should_ComputeStreakOnLocalDates_When_OffsetNonZero()
    {
        // Three sprints on three consecutive UTC days, all ending at 23:30 UTC.
        // With offset=+120, 23:30 UTC maps to 01:30 the NEXT local day.
        // So three sprints at 23:30 UTC on days D, D-1, D-2 appear on local days D+1, D, D-1.
        // The streak should still be >= 1 (today local has a sprint from yesterday's UTC sprint).

        // Guard: only reliable when UtcNow.Hour < 22.
        if (DateTime.UtcNow.Hour >= 22)
            return;

        var utcBase = DateTime.UtcNow.Date.AddHours(23).AddMinutes(30); // 23:30 UTC today

        // These map to local days: today+1, today, today-1 (UTC+2)
        _sessionRepository.Sessions.Add(FreeSprintCompleted(utcBase.AddDays(-2).AddMinutes(-25), 25)); // local day: yesterday
        _sessionRepository.Sessions.Add(FreeSprintCompleted(utcBase.AddDays(-1).AddMinutes(-25), 25)); // local day: today
        // No sprint for utcBase (which would be local tomorrow — outside today's window)

        var result = await _sut.Handle(new GetFocusSummaryQuery(OffsetMinutes: 120));

        // Today local has 1 sprint (from yesterday UTC 23:30 => local today 01:30).
        // Yesterday local has 1 sprint (from 2 days ago UTC 23:30 => local yesterday 01:30).
        // Streak = 2.
        Assert.Equal(2, result.Streak);
    }
}
