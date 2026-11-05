using KairuFocus.Domain.Community;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// ADR-026 guard on the SQL of the reads across users about open sprints: members only, never the Users
/// table, no email nor real name, no user identifier in the projection. <c>OwnerId</c> only appears in the
/// join condition. Per-row deadlines stay out of SQL.
/// </summary>
public sealed class OpenSprintReadSqlTests
{
    private const string OpenGroups = nameof(IOpenSprintReadRepository.GetOpenGroupsAsync);
    private const string Joinable = nameof(IOpenSprintReadRepository.FindJoinableAsync);
    private const string Participants = nameof(IOpenSprintReadRepository.GetParticipantsAsync);

    private static readonly DateTime Now = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Participants)]
    public async Task Should_NotReferenceUsersEmailOrDisplayName_When_Queried(string method)
    {
        var command = await CaptureAsync(method);

        Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Email", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DisplayName", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Participants)]
    public async Task Should_NotProjectOwnerIdOrUserId_When_Queried(string method)
    {
        var command = await CaptureAsync(method);

        Assert.All(command.OuterProjection, expression =>
        {
            Assert.DoesNotContain("OwnerId", expression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UserId", expression, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Participants)]
    public async Task Should_ProjectOnlySprintColumnsNameAndAvatar_When_Queried(string method)
    {
        var command = await CaptureAsync(method);

        Assert.Collection(
            command.OuterProjection,
            id => Assert.EndsWith(".[Id]", id, StringComparison.Ordinal),
            root => Assert.EndsWith(".[SharedSprintId]", root, StringComparison.Ordinal),
            startedAt => Assert.EndsWith(".[StartedAt]", startedAt, StringComparison.Ordinal),
            planned => Assert.EndsWith(".[PlannedDurationMinutes]", planned, StringComparison.Ordinal),
            sharedEnd => Assert.EndsWith(".[SharedEndAt]", sharedEnd, StringComparison.Ordinal),
            name => Assert.EndsWith(".[ProfileName]", name, StringComparison.Ordinal),
            avatar => Assert.EndsWith(".[AvatarUrl]", avatar, StringComparison.Ordinal));
    }

    // OwnerId and UserId appear exactly once each, on either side of the join condition.
    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Participants)]
    public async Task Should_UseOwnerIdOnlyInJoinCondition_When_Queried(string method)
    {
        var command = await CaptureAsync(method);

        Assert.Matches(
            @"INNER JOIN \[MemberProfiles\] AS \[\w+\] ON \[\w+\]\.\[OwnerId\] = \[\w+\]\.\[UserId\]",
            command.Text);
        Assert.Single(Occurrences(command.Text, "OwnerId"));
        Assert.Single(Occurrences(command.Text, "UserId"));
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Participants)]
    public async Task Should_KeepDeadlineRulesOutOfSql_When_Queried(string method)
    {
        var command = await CaptureAsync(method);

        Assert.DoesNotContain("DATEADD", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DATEDIFF", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    public async Task Should_CheckClosureOnSessionAndRoot_When_ReadingOpenSprints(string method)
    {
        var command = await CaptureAsync(method);

        Assert.Matches(
            @"NOT EXISTS \(\s*SELECT 1\s*FROM \[PomodoroSessions\] AS \[(?<root>\w+)\]\s*" +
            @"WHERE \[\k<root>\]\.\[IsClosedToJoin\] = CAST\(1 AS bit\) AND " +
            @"\(\[\k<root>\]\.\[Id\] = \[\w+\]\.\[Id\] OR \[\k<root>\]\.\[Id\] = \[\w+\]\.\[SharedSprintId\]\)\)",
            command.Text);
    }

    // Those already present stay visible to each other after the host closes the group.
    [Fact]
    public async Task Should_IgnoreClosure_When_ReadingParticipants()
    {
        var command = await CaptureAsync(Participants);

        Assert.DoesNotContain("IsClosedToJoin", command.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_LookUpTheNameKey_When_ReadingJoinableSprint()
    {
        var command = await CaptureAsync(Joinable);

        Assert.Matches(@"\[\w+\]\.\[NameKey\] = @\w+", command.Text);
        Assert.Contains(command.Parameters, p => Equals(p.Value, "lea_dev"));
    }

    private static IEnumerable<int> Occurrences(string text, string term)
    {
        for (var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
             index >= 0;
             index = text.IndexOf(term, index + term.Length, StringComparison.OrdinalIgnoreCase))
        {
            yield return index;
        }
    }

    private static Task<CapturedCommand> CaptureAsync(string method) =>
        SqlCommandCapture.CaptureAsync(context =>
        {
            var repository = new EfCoreOpenSprintReadRepository(context);
            return method switch
            {
                OpenGroups => repository.GetOpenGroupsAsync(Now, 10),
                Joinable => repository.FindJoinableAsync(ProfileName.Create("LEA_dev").Value, Now),
                Participants => repository.GetParticipantsAsync(Guid.NewGuid(), Now),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        });
}
