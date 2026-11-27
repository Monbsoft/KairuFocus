using KairuFocus.Application.Pomodoro.Queries.GetSuggestedSessionType;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetSuggestedSessionTypeQueryHandlerTests
{
    // The handler's clock is pinned (2026-06-26 12:00 UTC by default), so the local day is
    // deterministic whatever the time the tests run at.
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly GetSuggestedSessionTypeQueryHandler _sut;

    // Successive sessions added by AddCompleted(type, count) end 30 minutes apart, from 08:00 UTC
    // on the day of FixedNow, so that "the latest one" is unambiguous.
    private int _sequence;

    public GetSuggestedSessionTypeQueryHandlerTests()
    {
        _sut = BuildSut(FixedNow);
    }

    private GetSuggestedSessionTypeQueryHandler BuildSut(DateTimeOffset now) =>
        new(
            _sessionRepository,
            _settingsRepository,
            new FakeCurrentUserService(),
            NullLogger<GetSuggestedSessionTypeQueryHandler>.Instance,
            new FixedTimeProvider(now));

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private void AddCompleted(PomodoroSessionType type, DateTime endedAt)
    {
        var s = PomodoroSession.Create(type, 25, FakeCurrentUserService.TestUserId);
        s.Start(endedAt.AddMinutes(-25));
        s.Complete(endedAt);
        _sessionRepository.Sessions.Add(s);
    }

    private void AddCompleted(PomodoroSessionType type, int count = 1)
    {
        for (var i = 0; i < count; i++)
            AddCompleted(type, FixedNow.UtcDateTime.Date.AddHours(8).AddMinutes(30 * _sequence++));
    }

    // ── Nominal tests (UTC, offset=0) ─────────────────────────────────────

    [Fact]
    public async Task Should_SuggestSprint_When_NoSessionsToday()
    {
        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestShortBreak_When_LastWasFirstSprint()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.ShortBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestLongBreak_When_FourSprintsWithoutBreak()
    {
        AddCompleted(PomodoroSessionType.Sprint, 4);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.LongBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestSprint_When_LastWasABreak()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestShortBreak_When_ThreeSprintsOneBreak()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.ShortBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestLongBreak_When_FourSprintsThreeBreaks()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.LongBreak, result.SuggestedType);
    }

    // ── Offset / local-day test ────────────────────────────────────────────

    [Fact]
    public async Task Should_UseLocalDayWindow_When_OffsetNonZero()
    {
        // Now = 2026-06-26 12:00 UTC, offset = +120 (UTC+2).
        // Local window: startUtc = 2026-06-25 22:00 UTC, endUtc = 2026-06-26 22:00 UTC.
        // Four sprints ending at 2026-06-26 10:00 UTC (= 12:00 local) are all in that window.
        var sprintEndedAt = Utc(2026, 6, 26, 10, 0);
        for (var i = 0; i < 4; i++)
            AddCompleted(PomodoroSessionType.Sprint, sprintEndedAt);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: 120));

        // 4 sprints today local => multiple of SprintsBeforeLongBreak (4) => LongBreak.
        Assert.Equal(PomodoroSessionType.LongBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestShortBreak_When_SprintEndedAfterLocalMidnightWithPositiveOffset()
    {
        // Now = 2026-06-27 00:30 UTC = 01:30 local on the 27th (UTC+1): the local day starts at 2026-06-26 23:00 UTC.
        // The sprint ended at 23:10 UTC = 00:10 local on the 27th: it belongs to today (UTC day: yesterday).
        AddCompleted(PomodoroSessionType.Sprint, Utc(2026, 6, 26, 23, 10));

        var result = await BuildSut(new DateTimeOffset(2026, 6, 27, 0, 30, 0, TimeSpan.Zero))
            .Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: 60));

        Assert.Equal(PomodoroSessionType.ShortBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestSprint_When_SprintEndedBeforeLocalMidnightWithPositiveOffset()
    {
        // Same day as above, but the sprint ended at 22:50 UTC = 23:50 local on the 26th: it is yesterday.
        AddCompleted(PomodoroSessionType.Sprint, Utc(2026, 6, 26, 22, 50));

        var result = await BuildSut(new DateTimeOffset(2026, 6, 27, 0, 30, 0, TimeSpan.Zero))
            .Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: 60));

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestShortBreak_When_SprintEndedOnSameLocalDayWithNegativeOffset()
    {
        // Now = 2026-06-27 01:00 UTC = 20:00 local on the 26th (UTC-5): the local day starts at 2026-06-26 05:00 UTC.
        // The sprint ended at 2026-06-26 23:50 UTC = 18:50 local on the 26th: it belongs to today.
        AddCompleted(PomodoroSessionType.Sprint, Utc(2026, 6, 26, 23, 50));

        var result = await BuildSut(new DateTimeOffset(2026, 6, 27, 1, 0, 0, TimeSpan.Zero))
            .Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: -300));

        Assert.Equal(PomodoroSessionType.ShortBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestSprint_When_SprintEndedOnPreviousLocalDayWithNegativeOffset()
    {
        // Now = 2026-06-26 06:00 UTC = 01:00 local on the 26th (UTC-5): the local day starts at 2026-06-26 05:00 UTC.
        // The sprint ended at 04:50 UTC = 23:50 local on the 25th: it is yesterday (UTC day: today).
        AddCompleted(PomodoroSessionType.Sprint, Utc(2026, 6, 26, 4, 50));

        var result = await BuildSut(new DateTimeOffset(2026, 6, 26, 6, 0, 0, TimeSpan.Zero))
            .Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: -300));

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    // ── Clamp test (correction E) ──────────────────────────────────────────

    [Fact]
    public async Task Should_NotThrow_When_OffsetMinutesIsOutOfRange()
    {
        // offsetMinutes = 1440 is outside the valid range (max +840).
        // The handler must clamp it and not throw or produce an invalid window.
        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: 1440));

        // Result should be a valid suggestion (Sprint since no sessions exist).
        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestSprint_When_LateCompletedSprintEndedOnPreviousDay()
    {
        // Started two days ago, completion reported now: EndedAt is retained at the scheduled end
        // (two days ago), so no sprint counts as completed today.
        var start = FixedNow.UtcDateTime.AddDays(-2);
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, FakeCurrentUserService.TestUserId);
        session.Start(start);
        session.Complete(FixedNow.UtcDateTime);
        _sessionRepository.Sessions.Add(session);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }
}
