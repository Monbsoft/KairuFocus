using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityRecap;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class GetCommunityRecapQueryHandlerTests
{
    // Thursday 2026-10-22 14:00 UTC → week [Mon 2026-10-19, Mon 2026-10-26)
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 14, 0, 0, TimeSpan.Zero);

    private readonly FakeCommunityActivityRepository _activityRepository = new();
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly GetCommunityRecapQueryHandler _sut;

    public GetCommunityRecapQueryHandlerTests()
    {
        _sut = new GetCommunityRecapQueryHandler(
            _activityRepository,
            _sessionRepository,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now),
            NullLogger<GetCommunityRecapQueryHandler>.Instance);
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private void AddSession(DateTime startedAt, PomodoroSessionType type = PomodoroSessionType.Sprint, UserId? owner = null)
    {
        var session = PomodoroSession.Create(type, 25, owner ?? FakeCurrentUserService.TestUserId);
        session.Start(startedAt);
        session.Complete(startedAt.AddMinutes(25));
        _sessionRepository.Sessions.Add(session);
    }

    [Fact]
    public async Task Should_QueryCalendarWeekWindow_When_PeriodIsWeek()
    {
        await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Week));

        var expected = (Utc(2026, 10, 19), Utc(2026, 10, 26));
        Assert.Equal(expected, Assert.Single(_activityRepository.TotalsWindows));
        Assert.Equal(expected, Assert.Single(_activityRepository.ActiveMembersWindows));
        Assert.Equal(0, _activityRepository.FirstXpGainCallCount);
    }

    [Fact]
    public async Task Should_QueryCalendarMonthWindow_When_PeriodIsMonth()
    {
        await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Month));

        var expected = (Utc(2026, 10, 1), Utc(2026, 11, 1));
        Assert.Equal(expected, Assert.Single(_activityRepository.TotalsWindows));
        Assert.Equal(expected, Assert.Single(_activityRepository.ActiveMembersWindows));
    }

    [Fact]
    public async Task Should_StartAtFirstXpGainEver_When_PeriodIsGlobal()
    {
        var firstXpGainEver = Utc(2026, 7, 16, 9, 12);
        _activityRepository.FirstXpGainUtc = firstXpGainEver;

        await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Global));

        var expected = (firstXpGainEver, DateTime.MaxValue);
        Assert.Equal(expected, Assert.Single(_activityRepository.TotalsWindows));
        Assert.Equal(expected, Assert.Single(_activityRepository.ActiveMembersWindows));
    }

    [Fact]
    public async Task Should_ReturnZeros_When_NoXpWasEverEarned()
    {
        _activityRepository.FirstXpGainUtc = null;
        _activityRepository.Totals = new CompletedSprintTotals(5, TimeSpan.FromHours(2));
        _activityRepository.ActiveMembers = 3;
        AddSession(Utc(2026, 10, 20, 9));

        var result = await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Global));

        Assert.Equal(new GetCommunityRecapResult(Sprints: 0, FocusMinutes: 0, ActiveMembers: 0, MySprints: 0), result);
        Assert.Empty(_activityRepository.TotalsWindows);
        Assert.Empty(_activityRepository.ActiveMembersWindows);
    }

    [Fact]
    public async Task Should_CountMySprintsOnSameWindow_When_UserHasSprints()
    {
        AddSession(Utc(2026, 10, 16, 9));                                   // previous week
        AddSession(Utc(2026, 10, 19, 9));
        AddSession(Utc(2026, 10, 21, 9));
        AddSession(Utc(2026, 10, 21, 10), PomodoroSessionType.ShortBreak);  // a break is not a sprint
        AddSession(Utc(2026, 10, 21, 11), owner: UserId.New());             // someone else's sprint

        var result = await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Week));

        Assert.Equal(2, result.MySprints);
    }

    [Fact]
    public async Task Should_CountMySprintsSinceFirstXpGainEver_When_PeriodIsGlobal()
    {
        _activityRepository.FirstXpGainUtc = Utc(2026, 10, 5, 9, 25);
        AddSession(Utc(2026, 10, 1, 9));   // before XP went live
        AddSession(Utc(2026, 10, 6, 9));
        AddSession(Utc(2026, 10, 21, 9));

        var result = await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Global));

        Assert.Equal(2, result.MySprints);
    }

    [Fact]
    public async Task Should_RoundFocusMinutesOnce_When_MappingTotals()
    {
        _activityRepository.Totals = new CompletedSprintTotals(3, TimeSpan.FromSeconds(61 * 60 + 29));
        _activityRepository.ActiveMembers = 2;

        var result = await _sut.Handle(new GetCommunityRecapQuery(CommunityPeriod.Week));

        Assert.Equal(3, result.Sprints);
        Assert.Equal(61, result.FocusMinutes);
        Assert.Equal(2, result.ActiveMembers);
    }
}
