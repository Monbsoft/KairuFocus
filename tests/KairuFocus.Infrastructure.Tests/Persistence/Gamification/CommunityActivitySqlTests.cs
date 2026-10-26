using System.Data;
using KairuFocus.Domain.Gamification;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

/// <summary>
/// ADR-024 guard on the SQL of the anonymous community aggregates: never the Users table, OwnerId never
/// projected (it only filters and feeds the distinct count), per-row rules kept out of SQL.
/// </summary>
public sealed class CommunityActivitySqlTests
{
    private const string SprintsInProgress = nameof(ICommunityActivityRepository.CountSprintsInProgressAsync);
    private const string ActiveMembers = nameof(ICommunityActivityRepository.CountActiveMembersAsync);
    private const string CompletedSprintTotals = nameof(ICommunityActivityRepository.GetCompletedSprintTotalsAsync);
    private const string FirstXpGain = nameof(ICommunityActivityRepository.GetFirstXpGainUtcAsync);

    private static readonly DateTime WeekStart = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(SprintsInProgress)]
    [InlineData(ActiveMembers)]
    [InlineData(CompletedSprintTotals)]
    [InlineData(FirstXpGain)]
    public async Task Should_NotReferenceUsersTable_When_CommunityActivityIsQueried(string method)
    {
        var command = await CaptureAsync(method, WeekStart.AddDays(7));

        Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_NotProjectOwnerId_When_SummingCompletedSprints()
    {
        var command = await CaptureAsync(CompletedSprintTotals, WeekStart.AddDays(7));

        Assert.Collection(
            command.OuterProjection,
            startedAt => Assert.EndsWith(".[StartedAt]", startedAt, StringComparison.Ordinal),
            endedAt => Assert.EndsWith(".[EndedAt]", endedAt, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_NotProjectOwnerId_When_CountingSprintsInProgress()
    {
        var command = await CaptureAsync(SprintsInProgress, WeekStart.AddDays(7));

        Assert.Collection(
            command.OuterProjection,
            startedAt => Assert.EndsWith(".[StartedAt]", startedAt, StringComparison.Ordinal),
            planned => Assert.EndsWith(".[PlannedDurationMinutes]", planned, StringComparison.Ordinal));
    }

    // OwnerId may only appear inside the distinct count: the query returns a single number.
    [Fact]
    public async Task Should_ReturnOnlyACount_When_CountingActiveMembers()
    {
        var command = await CaptureAsync(ActiveMembers, WeekStart.AddDays(7));

        var count = Assert.Single(command.OuterProjection);
        Assert.StartsWith("COUNT(", count, StringComparison.Ordinal);
    }

    // Per-row rules (4 h cap, presence deadline) belong to CommunityRules, not to provider-specific SQL.
    [Theory]
    [InlineData(SprintsInProgress)]
    [InlineData(ActiveMembers)]
    [InlineData(CompletedSprintTotals)]
    public async Task Should_NotUseDateFunctions_When_CommunityActivityIsQueried(string method)
    {
        var command = await CaptureAsync(method, WeekStart.AddDays(7));

        Assert.DoesNotContain("DATEADD", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DATEDIFF", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    // The global recap passes DateTime.MaxValue as its end bound: as a legacy datetime parameter it would
    // overflow (datetime stops at 9999-12-31 23:59:59.997), as datetime2 it is valid.
    [Theory]
    [InlineData(ActiveMembers)]
    [InlineData(CompletedSprintTotals)]
    public async Task Should_SendEndBoundAsDateTime2_When_WindowIsOpenEnded(string method)
    {
        var command = await CaptureAsync(method, DateTime.MaxValue);

        var endBound = Assert.Single(command.Parameters, p => p.Value is DateTime value && value == DateTime.MaxValue);
        Assert.Equal(DbType.DateTime2, endBound.DbType);
    }

    private static Task<CapturedCommand> CaptureAsync(string method, DateTime endUtc) =>
        SqlCommandCapture.CaptureAsync(context =>
        {
            var repository = new EfCoreCommunityActivityRepository(context);
            return method switch
            {
                SprintsInProgress => repository.CountSprintsInProgressAsync(WeekStart.AddHours(12)),
                ActiveMembers => repository.CountActiveMembersAsync(WeekStart, endUtc),
                CompletedSprintTotals => repository.GetCompletedSprintTotalsAsync(WeekStart, endUtc),
                FirstXpGain => repository.GetFirstXpGainUtcAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        });
}
