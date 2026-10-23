using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityProgress;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class GetCommunityProgressQueryHandlerTests
{
    // Thursday 2026-10-22 14:00 UTC → week [Mon 2026-10-19, Mon 2026-10-26)
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 14, 0, 0, TimeSpan.Zero);
    private static readonly UserId Me = FakeCurrentUserService.TestUserId;

    private readonly FakeXpGainRepository _xpGainRepository = new();
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly GetCommunityProgressQueryHandler _sut;

    public GetCommunityProgressQueryHandlerTests()
    {
        _sut = new GetCommunityProgressQueryHandler(
            _xpGainRepository,
            _sessionRepository,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now),
            NullLogger<GetCommunityProgressQueryHandler>.Instance);
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    /// <summary>Completed sprint; a free sprint (planned 0) keeps its full duration.</summary>
    private void AddSprint(DateTime startedAt, TimeSpan duration, int plannedMinutes = 0, UserId? owner = null)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedMinutes, owner ?? Me);
        session.Start(startedAt);
        session.Complete(startedAt + duration);
        _sessionRepository.Sessions.Add(session);
    }

    /// <summary>25-minute timed sprint credited with 10 XP at its end, like CreditSprintXp does.</summary>
    private void AddCreditedSprint(DateTime startedAt, UserId? owner = null)
    {
        AddSprint(startedAt, TimeSpan.FromMinutes(25), plannedMinutes: 25, owner);
        _xpGainRepository.Gains.Add(XpGain.Credit(owner ?? Me, Guid.NewGuid(), 10, startedAt.AddMinutes(25)));
    }

    [Fact]
    public async Task Should_ReturnWeekXpSprintsAndCappedFocus_When_PeriodIsWeek()
    {
        AddCreditedSprint(Utc(2026, 10, 20, 9));
        AddSprint(Utc(2026, 10, 21, 4), TimeSpan.FromHours(6)); // forgotten free sprint: 4 h of focus
        _xpGainRepository.Gains.Add(XpGain.Credit(Me, Guid.NewGuid(), 10, Utc(2026, 10, 21, 10)));

        var result = await _sut.Handle(new GetCommunityProgressQuery(CommunityPeriod.Week));

        Assert.Equal(new DateOnly(2026, 10, 19), result.WindowStart);
        Assert.Equal(20, result.Xp);
        Assert.Equal(2, result.Sprints);
        Assert.Equal(25 + 240, result.FocusMinutes);
    }

    [Fact]
    public async Task Should_CountSprintWithoutXp_When_SprintLastsUnder5Minutes()
    {
        AddSprint(Utc(2026, 10, 20, 9), TimeSpan.FromMinutes(3));

        var result = await _sut.Handle(new GetCommunityProgressQuery(CommunityPeriod.Week));

        Assert.Equal(0, result.Xp);
        Assert.Equal(1, result.Sprints);
        Assert.Equal(3, result.FocusMinutes);
    }

    [Fact]
    public async Task Should_CountAllTimeSprintsSinceFirstGain_When_PeriodIsWeek()
    {
        AddSprint(Utc(2026, 10, 1, 9), TimeSpan.FromMinutes(25), plannedMinutes: 25); // before the first gain
        AddCreditedSprint(Utc(2026, 10, 5, 9));                                       // first gain at 09:25
        AddCreditedSprint(Utc(2026, 10, 20, 9));

        var result = await _sut.Handle(new GetCommunityProgressQuery(CommunityPeriod.Week));

        Assert.Equal(1, result.Sprints);
        Assert.Equal(new AllTimeViewModel(
            TotalXp: 20,
            Sprints: 2,
            FirstGainAt: new DateTimeOffset(2026, 10, 5, 9, 25, 0, TimeSpan.Zero)), result.AllTime);
    }

    [Fact]
    public async Task Should_UseFirstGainAsWindowStart_When_PeriodIsGlobal()
    {
        AddSprint(Utc(2026, 7, 10, 9), TimeSpan.FromMinutes(25), plannedMinutes: 25); // before the first gain
        AddCreditedSprint(Utc(2026, 7, 16, 9));
        AddCreditedSprint(Utc(2026, 9, 2, 9));
        AddSprint(Utc(2026, 10, 20, 9), TimeSpan.FromMinutes(3));

        var result = await _sut.Handle(new GetCommunityProgressQuery(CommunityPeriod.Global));

        Assert.Equal(new DateOnly(2026, 7, 16), result.WindowStart);
        Assert.Equal(20, result.Xp);
        Assert.Equal(3, result.Sprints);
        Assert.Equal(25 + 25 + 3, result.FocusMinutes);
        Assert.Equal(3, result.AllTime.Sprints);
        Assert.Null(result.Comparison);
    }

    [Fact]
    public async Task Should_ReturnZeros_When_UserHasNoGainAndPeriodIsGlobal()
    {
        AddSprint(Utc(2026, 10, 20, 9), TimeSpan.FromMinutes(3));

        var result = await _sut.Handle(new GetCommunityProgressQuery(CommunityPeriod.Global));

        Assert.Null(result.WindowStart);
        Assert.Equal(0, result.Xp);
        Assert.Equal(0, result.Sprints);
        Assert.Equal(0, result.FocusMinutes);
        Assert.Null(result.AverageWeeklyXp);
        Assert.Null(result.BestWeek);
        Assert.Null(result.BestMonth);
        Assert.Empty(result.Bars);
        Assert.Equal(new AllTimeViewModel(0, 0, null), result.AllTime);
    }

    [Fact]
    public async Task Should_IgnoreOtherUsersGainsAndSprints_When_ComputingProgress()
    {
        var someoneElse = UserId.New();
        AddCreditedSprint(Utc(2026, 10, 1, 9), someoneElse);
        AddCreditedSprint(Utc(2026, 10, 20, 9), someoneElse);
        AddSprint(Utc(2026, 10, 21, 9), TimeSpan.FromHours(1), owner: someoneElse);
        AddCreditedSprint(Utc(2026, 10, 21, 13));

        var result = await _sut.Handle(new GetCommunityProgressQuery(CommunityPeriod.Week));

        Assert.Equal(10, result.Xp);
        Assert.Equal(1, result.Sprints);
        Assert.Equal(25, result.FocusMinutes);
        Assert.Equal(new AllTimeViewModel(
            TotalXp: 10,
            Sprints: 1,
            FirstGainAt: new DateTimeOffset(2026, 10, 21, 13, 25, 0, TimeSpan.Zero)), result.AllTime);
    }
}
