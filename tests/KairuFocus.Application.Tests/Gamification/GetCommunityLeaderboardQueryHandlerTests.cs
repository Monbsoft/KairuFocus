using System.Text.Json;
using System.Text.RegularExpressions;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class GetCommunityLeaderboardQueryHandlerTests
{
    // Thursday 2026-10-22 14:00 UTC
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime JoinedAtUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private static readonly Regex AnyGuid = new(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.CultureInvariant);

    private readonly FakeLeaderboardProfileRepository _profileRepository = new();
    private readonly FakeLeaderboardStandingsRepository _standingsRepository = new();
    private readonly GetCommunityLeaderboardQueryHandler _sut;

    public GetCommunityLeaderboardQueryHandlerTests()
    {
        _sut = new GetCommunityLeaderboardQueryHandler(
            _profileRepository,
            _standingsRepository,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now),
            NullLogger<GetCommunityLeaderboardQueryHandler>.Instance);
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_QueryCurrentWeekWindow_When_PeriodIsWeek()
    {
        await _sut.Handle(new GetCommunityLeaderboardQuery(CommunityPeriod.Week));

        var window = Assert.Single(_standingsRepository.BetweenWindows);
        Assert.Equal((Utc(2026, 10, 19), Utc(2026, 10, 26)), window);
        Assert.Equal(0, _standingsRepository.AllTimeCallCount);
    }

    [Fact]
    public async Task Should_QueryCalendarMonthWindow_When_PeriodIsMonth()
    {
        await _sut.Handle(new GetCommunityLeaderboardQuery(CommunityPeriod.Month));

        var window = Assert.Single(_standingsRepository.BetweenWindows);
        Assert.Equal((Utc(2026, 10, 1), Utc(2026, 11, 1)), window);
    }

    [Fact]
    public async Task Should_QueryAllTimeStandings_When_PeriodIsGlobal()
    {
        await _sut.Handle(new GetCommunityLeaderboardQuery(CommunityPeriod.Global));

        Assert.Equal(1, _standingsRepository.AllTimeCallCount);
        Assert.Empty(_standingsRepository.BetweenWindows);
    }

    [Fact]
    public async Task Should_ReturnBoardWithoutMe_When_UserIsNotParticipant()
    {
        _profileRepository.Seed(UserId.New(), "LoutreTenace", JoinedAtUtc);
        _standingsRepository.Add("LoutreTenace", 210);
        _standingsRepository.Add("HibouAttentif", 180);

        var result = await _sut.Handle(new GetCommunityLeaderboardQuery(CommunityPeriod.Week));

        Assert.False(result.IsParticipant);
        Assert.Null(result.Me);
        Assert.Equal(2, result.RankedCount);
        Assert.Equal(["LoutreTenace", "HibouAttentif"], result.Top.Select(r => r.Pseudonym));
    }

    [Fact]
    public async Task Should_FindMyRowByPseudonym_When_UserIsParticipant()
    {
        _profileRepository.Seed(FakeCurrentUserService.TestUserId, "RenardAgile", JoinedAtUtc);
        _standingsRepository.Add("LoutreTenace", 210);
        _standingsRepository.Add("RenardAgile", 120);

        var result = await _sut.Handle(new GetCommunityLeaderboardQuery(CommunityPeriod.Week));

        Assert.True(result.IsParticipant);
        Assert.Equal(new MyStandingViewModel(Rank: 2, IsTied: false, Xp: 120, Pseudonym: "RenardAgile", IsInTop: true), result.Me);
    }

    [Fact]
    public async Task Should_NotSerializeAnyUserId_When_ResultIsSerialized()
    {
        _profileRepository.Seed(FakeCurrentUserService.TestUserId, "RenardAgile", JoinedAtUtc);
        _profileRepository.Seed(UserId.New(), "LoutreTenace", JoinedAtUtc);
        _standingsRepository.Add("LoutreTenace", 210);
        _standingsRepository.Add("RenardAgile", 120);

        var result = await _sut.Handle(new GetCommunityLeaderboardQuery(CommunityPeriod.Week));
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"pseudonym\":\"RenardAgile\"", json);
        Assert.DoesNotMatch(AnyGuid, json);
        Assert.DoesNotContain(FakeCurrentUserService.TestUserId.Value.ToString(), json, StringComparison.OrdinalIgnoreCase);
    }
}
