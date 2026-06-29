using KairuFocus.Application.Pomodoro.Queries.GetFocusStats;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class GetFocusStatsQueryHandlerTests
{
    // Fixed instant: 2026-06-26 12:00:00 UTC — noon, far from midnight.
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime FixedUtcNow = FixedNow.UtcDateTime;

    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly GetFocusStatsQueryHandler _sut;

    public GetFocusStatsQueryHandlerTests()
    {
        _sut = new GetFocusStatsQueryHandler(
            _sessionRepository,
            new FakeCurrentUserService(),
            NullLogger<GetFocusStatsQueryHandler>.Instance,
            new FixedTimeProvider(FixedNow));
    }

    private static PomodoroSession SprintCompleted(DateTime startedAt, int durationMinutes)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Complete(startedAt.AddMinutes(durationMinutes));
        return session;
    }

    [Fact]
    public async Task Should_ReturnEmptyDays_When_NoSprints()
    {
        var result = await _sut.Handle(new GetFocusStatsQuery());

        Assert.Empty(result.Days);
    }

    [Fact]
    public async Task Should_AggregateSprintsPerDay_When_Queried()
    {
        var todayAt9 = FixedUtcNow.Date.AddHours(9);
        _sessionRepository.Sessions.Add(SprintCompleted(todayAt9, 25));
        _sessionRepository.Sessions.Add(SprintCompleted(todayAt9.AddHours(1), 25));
        _sessionRepository.Sessions.Add(SprintCompleted(todayAt9.AddDays(-1), 10));

        var result = await _sut.Handle(new GetFocusStatsQuery());

        Assert.Equal(2, result.Days.Count);
        var today = result.Days.Single(d => d.Date == new DateOnly(2026, 6, 26));
        Assert.Equal(2, today.SprintCount);
        Assert.Equal(50, today.FocusMinutes);
        var yesterday = result.Days.Single(d => d.Date == new DateOnly(2026, 6, 25));
        Assert.Equal(1, yesterday.SprintCount);
        Assert.Equal(10, yesterday.FocusMinutes);
    }

    [Fact]
    public async Task Should_ExcludeSprintsOlderThanLookback_When_Aggregating()
    {
        // sinceUtc = (2026-06-26 00:00 UTC) - 365 jours = 2025-06-26 00:00 UTC.
        // Un sprint terminé avant cette borne ne doit pas apparaître.
        _sessionRepository.Sessions.Add(SprintCompleted(new DateTime(2025, 6, 24, 9, 0, 0, DateTimeKind.Utc), 25));
        _sessionRepository.Sessions.Add(SprintCompleted(FixedUtcNow.Date.AddHours(9), 25));

        var result = await _sut.Handle(new GetFocusStatsQuery());

        var day = Assert.Single(result.Days);
        Assert.Equal(new DateOnly(2026, 6, 26), day.Date);
    }

    [Fact]
    public async Task Should_FilterByCurrentUser_When_OtherUserHasSprints()
    {
        var other = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, UserId.From(Guid.NewGuid()));
        other.Start(FixedUtcNow.Date.AddHours(9));
        other.Complete(FixedUtcNow.Date.AddHours(9).AddMinutes(25));
        _sessionRepository.Sessions.Add(other);

        var result = await _sut.Handle(new GetFocusStatsQuery());

        Assert.Empty(result.Days);
    }

    [Fact]
    public async Task Should_BucketOnLocalDate_When_OffsetNonZero()
    {
        // Sprint ends 2026-06-25 23:30 UTC => with +120 => local 2026-06-26 01:30.
        _sessionRepository.Sessions.Add(SprintCompleted(new DateTime(2026, 6, 25, 23, 5, 0, DateTimeKind.Utc), 25));

        var result = await _sut.Handle(new GetFocusStatsQuery(OffsetMinutes: 120));

        var day = Assert.Single(result.Days);
        Assert.Equal(new DateOnly(2026, 6, 26), day.Date);
    }
}
