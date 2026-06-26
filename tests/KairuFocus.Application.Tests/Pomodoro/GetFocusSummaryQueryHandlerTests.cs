using KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetFocusSummaryQueryHandlerTests
{
    // Fixed instant: 2026-06-26 12:00:00 UTC — noon, far from midnight in both directions.
    // All tests anchor relative to this instant, making them fully deterministic.
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime FixedUtcNow = FixedNow.UtcDateTime;

    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly GetFocusSummaryQueryHandler _sut;

    public GetFocusSummaryQueryHandlerTests()
    {
        _sut = new GetFocusSummaryQueryHandler(
            _sessionRepository,
            _settingsRepository,
            new FakeCurrentUserService(),
            NullLogger<GetFocusSummaryQueryHandler>.Instance,
            new FixedTimeProvider(FixedNow));
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

    // ── Nominal tests (UTC, offset=0) ─────────────────────────────────────

    [Fact]
    public async Task Should_ReturnSprintsTodayCount_When_SprintsCompletedToday()
    {
        var todayStart = FixedUtcNow.Date; // 2026-06-26 00:00 UTC
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(9), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(10), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(2, result.SprintsToday);
    }

    [Fact]
    public async Task Should_SumDurations_When_ComputingFocusMinutesToday()
    {
        var todayStart = FixedUtcNow.Date;
        // 25 min + 10 min => 35 minutes
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(9), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayStart.AddHours(10), 10));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        Assert.Equal(35, result.FocusMinutesToday);
    }

    [Fact]
    public async Task Should_SumAllCompletedSprintDurations_When_ComputingFocusMinutesToday()
    {
        var todayStart = FixedUtcNow.Date;
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
        // Three sprints at 09:00 UTC on today, yesterday, and two days ago.
        var todayAt9 = FixedUtcNow.Date.AddHours(9); // 2026-06-26 09:00 UTC
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayAt9, 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayAt9.AddDays(-1), 25));
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayAt9.AddDays(-2), 25));

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
    // Fixed instant: 2026-06-26 12:00:00 UTC
    // With offset=+120 (UTC+2): local time is 14:00, local date is 2026-06-26.
    // startUtc = local midnight 2026-06-26 00:00 UTC+2 = 2026-06-25 22:00 UTC
    // endUtc   = local midnight 2026-06-27 00:00 UTC+2 = 2026-06-26 22:00 UTC
    // A sprint ending at 2026-06-26 23:30 UTC falls in [22:00, next day) => NEXT local day.
    // A sprint ending at 2026-06-26 21:00 UTC falls in [22:00 prev day, 22:00) => STILL today local.

    [Fact]
    public async Task Should_NotCountSprint_When_SprintEndedAtUtcNightButBelongsToTomorrowLocal()
    {
        // Sprint ends at 2026-06-26 23:30 UTC.
        // With offset=+120: local time = 2026-06-27 01:30 => next local day.
        // endUtc = 2026-06-26 22:00 UTC; 23:30 > 22:00, so NOT counted.
        var sprintEndedAtUtc = new DateTime(2026, 6, 26, 23, 30, 0, DateTimeKind.Utc);

        _sessionRepository.Sessions.Add(FreeSprintCompleted(sprintEndedAtUtc.AddMinutes(-25), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery(OffsetMinutes: 120));

        Assert.Equal(0, result.SprintsToday);
        Assert.Equal(0, result.FocusMinutesToday);
    }

    [Fact]
    public async Task Should_CountSprint_When_SprintEndsBeforeLocalMidnight_WithPositiveOffset()
    {
        // Sprint ends at 2026-06-26 21:00 UTC.
        // With offset=+120: local time = 2026-06-26 23:00 => still today local.
        // startUtc = 2026-06-25 22:00 UTC; endUtc = 2026-06-26 22:00 UTC.
        // 21:00 UTC is within [22:00 prev day, 22:00 today) => counted.
        var sprintEndedAtUtc = new DateTime(2026, 6, 26, 21, 0, 0, DateTimeKind.Utc);

        _sessionRepository.Sessions.Add(FreeSprintCompleted(sprintEndedAtUtc.AddMinutes(-25), 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery(OffsetMinutes: 120));

        Assert.Equal(1, result.SprintsToday);
    }

    [Fact]
    public async Task Should_ComputeStreakOnLocalDates_When_OffsetNonZero()
    {
        // Three sprints all ending at 23:30 UTC on 2026-06-26, 2026-06-25, 2026-06-24.
        // With offset=+120: 23:30 UTC => 01:30 next local day.
        //   23:30 on 2026-06-24 UTC => local 2026-06-25 01:30 (yesterday local)
        //   23:30 on 2026-06-25 UTC => local 2026-06-26 01:30 (today local)
        //   23:30 on 2026-06-26 UTC => local 2026-06-27 01:30 (tomorrow local — outside window)
        // Fixed now = 2026-06-26 12:00 UTC => today local = 2026-06-26.
        // Today local has sprint from 2026-06-25 23:30 UTC. Yesterday local from 2026-06-24 23:30 UTC.
        // Streak = 2 (today + yesterday).

        _sessionRepository.Sessions.Add(FreeSprintCompleted(new DateTime(2026, 6, 24, 23, 5, 0, DateTimeKind.Utc), 25)); // ends 23:30 UTC 2026-06-24 => local yesterday
        _sessionRepository.Sessions.Add(FreeSprintCompleted(new DateTime(2026, 6, 25, 23, 5, 0, DateTimeKind.Utc), 25)); // ends 23:30 UTC 2026-06-25 => local today
        // Intentionally no sprint ending at 23:30 UTC 2026-06-26 (would be local tomorrow)

        var result = await _sut.Handle(new GetFocusSummaryQuery(OffsetMinutes: 120));

        Assert.Equal(2, result.Streak);
        // SprintsToday: only the sprint ending at 23:30 UTC 2026-06-25 (local 2026-06-26 01:30) is within
        // startUtc=2026-06-25 22:00 and endUtc=2026-06-26 22:00.
        Assert.Equal(1, result.SprintsToday);
    }

    // ── Lookback window tests ──────────────────────────────────────────────

    [Fact]
    public async Task Should_NotCountOldSprint_When_EndedAtIsOutsideLookbackWindow()
    {
        // Fixed now = 2026-06-26 12:00 UTC, offset = 0.
        // startUtc = 2026-06-26 00:00 UTC.
        // sinceUtc = startUtc - 366 days = 2025-06-25 00:00 UTC.
        // A sprint ending before sinceUtc must NOT be included in the streak computation.
        // It is NOT a sprint for today either, so SprintsToday and Streak should both be 0.

        // Sprint ending at 2025-06-24 12:00 UTC — one day before sinceUtc.
        var tooOldEndedAt = new DateTime(2025, 6, 24, 12, 0, 0, DateTimeKind.Utc);
        _sessionRepository.Sessions.Add(FreeSprintCompleted(tooOldEndedAt.AddMinutes(-25), 25));

        // Also add a sprint for today to confirm it IS counted (verifies the filter is additive, not exclusive).
        var todayAt9 = FixedUtcNow.Date.AddHours(9);
        _sessionRepository.Sessions.Add(FreeSprintCompleted(todayAt9, 25));

        var result = await _sut.Handle(new GetFocusSummaryQuery());

        // The old sprint is excluded from the streak window; today's sprint is included.
        Assert.Equal(1, result.SprintsToday);
        Assert.Equal(1, result.Streak); // only today counts; the ancient sprint is invisible
    }
}
