using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

/// <summary>
/// ADR-024 guard on the SQL of the cross-user leaderboard: community members only, (Name, AvatarUrl, Xp) only,
/// never the Users table, never a user identifier in the projection.
/// </summary>
public sealed class LeaderboardStandingsSqlTests
{
    private const string Period = "period";
    private const string AllTime = "all-time";

    private static readonly DateTime WeekStart = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_NotReferenceUsersTable_When_PeriodStandingsAreQueried()
    {
        var command = await CaptureAsync(Period);

        Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_NotReferenceUsersTable_When_AllTimeStandingsAreQueried()
    {
        var command = await CaptureAsync(AllTime);

        Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(Period)]
    [InlineData(AllTime)]
    public async Task Should_ProjectOnlyNameAvatarAndXpSum_When_StandingsAreQueried(string standings)
    {
        var command = await CaptureAsync(standings);

        var projection = command.OuterProjection;
        Assert.Collection(
            projection,
            name => Assert.EndsWith(".[ProfileName]", name, StringComparison.Ordinal),
            avatar => Assert.EndsWith(".[AvatarUrl]", avatar, StringComparison.Ordinal),
            xp => Assert.Contains("SUM(", xp, StringComparison.Ordinal));
        Assert.Contains("[Amount]", projection[2], StringComparison.Ordinal);
        Assert.All(projection, expression =>
        {
            Assert.DoesNotContain("OwnerId", expression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UserId", expression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SessionId", expression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("[Id]", expression, StringComparison.OrdinalIgnoreCase);
        });
    }

    // GroupBy on a value-converted property (Name) and the avatar must translate to a plain GROUP BY on their columns.
    [Theory]
    [InlineData(Period)]
    [InlineData(AllTime)]
    public async Task Should_InnerJoinMemberProfilesAndGroupByNameAndAvatar_When_StandingsAreQueried(string standings)
    {
        var command = await CaptureAsync(standings);

        Assert.Contains("INNER JOIN [MemberProfiles]", command.Text, StringComparison.Ordinal);
        Assert.Matches(@"GROUP BY \[\w+\]\.\[ProfileName\], \[\w+\]\.\[AvatarUrl\]\s*$", command.Text);
    }

    private static Task<CapturedCommand> CaptureAsync(string standings) =>
        SqlCommandCapture.CaptureAsync(context =>
        {
            var repository = new EfCoreLeaderboardStandingsRepository(context);
            return standings switch
            {
                Period => repository.GetStandingsBetweenAsync(WeekStart, WeekStart.AddDays(7)),
                AllTime => repository.GetAllTimeStandingsAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(standings), standings, null),
            };
        });
}
