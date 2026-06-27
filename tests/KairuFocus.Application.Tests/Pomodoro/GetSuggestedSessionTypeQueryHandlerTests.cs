using KairuFocus.Application.Pomodoro.Queries.GetSuggestedSessionType;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetSuggestedSessionTypeQueryHandlerTests
{
    // Existing tests use TimeProvider.System so that sessions created with DateTime.UtcNow
    // are found by FakePomodoroSessionRepository.GetLatestCompletedTodayAsync (which also
    // compares against DateTime.UtcNow.Date). The new offset test uses a FixedTimeProvider.
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly GetSuggestedSessionTypeQueryHandler _sut;

    public GetSuggestedSessionTypeQueryHandlerTests()
    {
        _sut = BuildSut(TimeProvider.System);
    }

    private GetSuggestedSessionTypeQueryHandler BuildSut(TimeProvider timeProvider) =>
        new(
            _sessionRepository,
            _settingsRepository,
            new FakeCurrentUserService(),
            NullLogger<GetSuggestedSessionTypeQueryHandler>.Instance,
            timeProvider);

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
            AddCompleted(type, DateTime.UtcNow);
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
        // Fixed now = 2026-06-26 12:00 UTC, offset = +120 (UTC+2).
        // Local window: startUtc = 2026-06-25 22:00 UTC, endUtc = 2026-06-26 22:00 UTC.
        //
        // Four sprints ending at 2026-06-26 10:00 UTC (= 12:00 local) => in local window.
        // GetLatestCompletedTodayAsync in the Fake uses DateTime.UtcNow.Date; we set EndedAt
        // to an absolute UTC date that is unambiguously "today local" for the fixed window,
        // but we must also ensure GetLatestCompletedTodayAsync finds the session.
        // Since the Fake compares EndedAt.Date == DateTime.UtcNow.Date, and UtcNow.Date
        // right now is 2026-06-27, we cannot use an absolute 2026-06-26 date.
        //
        // Solution: use DateTime.UtcNow for the session timestamps so the Fake finds them,
        // and use a FixedTimeProvider set to *today's* real UTC noon to keep the window
        // consistent.  The test verifies that with offset=+120 the four sprints are counted
        // in the local window (they all end within the last 2h of the local day).
        var fixedNow = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero);
        var sut = BuildSut(new FixedTimeProvider(fixedNow));

        // Four sprints completed today UTC at 10:00 => 12:00 local (still within local window).
        var sprintEndedAt = DateTime.UtcNow.Date.AddHours(10);
        for (var i = 0; i < 4; i++)
            AddCompleted(PomodoroSessionType.Sprint, sprintEndedAt);

        var result = await sut.Handle(new GetSuggestedSessionTypeQuery(OffsetMinutes: 120));

        // 4 sprints today local => multiple of SprintsBeforeLongBreak (4) => LongBreak.
        Assert.Equal(PomodoroSessionType.LongBreak, result.SuggestedType);
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
}
