using System.Text.RegularExpressions;
using KairuFocus.Domain.Community;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// ADR-026 guard on the SQL of the reads across users about open sprints and group sessions: members only,
/// never the Users table, no email nor real name, no user identifier in any projection. <c>OwnerId</c>,
/// <c>HostId</c> and <c>UserId</c> only appear in join and filter conditions. Per-row deadlines stay out of SQL.
/// Every command of a call is checked: each read sends several queries.
/// </summary>
public sealed class OpenSprintReadSqlTests
{
    private const string OpenGroups = nameof(IOpenSprintReadRepository.GetOpenGroupsAsync);
    private const string Joinable = nameof(IOpenSprintReadRepository.FindJoinableAsync);
    private const string GroupMembers = nameof(IOpenSprintReadRepository.GetGroupMembersAsync);
    private const string Desks = nameof(IOpenSprintReadRepository.GetDesksAsync);
    private const string Desk = nameof(IOpenSprintReadRepository.GetDeskAsync);
    private const string HostAmbiance = nameof(IOpenSprintReadRepository.GetHostAmbianceCodeAsync);

    private static readonly DateTime Now = new(2026, 11, 8, 14, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Root = Guid.Parse("5f0c1a52-8d3e-4b7a-9c61-2e4d8f0b7a13");

    private static readonly string[] IdentifierColumns = ["OwnerId", "HostId", "UserId"];

    // Columns a cross-user read may return: session and step columns, closure, public name, avatar and ambiance,
    // counts.
    private static readonly Regex AllowedProjection = new(
        @"^(TOP\([@\w]+\) )?(\[\w+\]\.\[(Id|StartedAt|PlannedDurationMinutes|CurrentStepType|CurrentStepEndsAtUtc|GroupSessionId|JoinedAtUtc|IsClosedToJoin|ProfileName|AvatarUrl|AmbianceCode)\]|COUNT\(\*\))( AS \[\w+\])?$",
        RegexOptions.CultureInvariant);

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(GroupMembers)]
    [InlineData(Desks)]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_NotReferenceUsersEmailOrDisplayName_When_Queried(string method)
    {
        foreach (var command in await CaptureAsync(method))
        {
            Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Email", command.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DisplayName", command.Text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(GroupMembers)]
    [InlineData(Desks)]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_NotProjectOwnerIdHostIdOrUserId_When_Queried(string method)
    {
        foreach (var command in await CaptureAsync(method))
        {
            Assert.All(command.OuterProjection, expression =>
            {
                foreach (var column in IdentifierColumns)
                    Assert.DoesNotContain(column, expression, StringComparison.OrdinalIgnoreCase);
            });
        }
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(GroupMembers)]
    [InlineData(Desks)]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_ProjectOnlyStepColumnsNamesAndAvatars_When_Queried(string method)
    {
        foreach (var command in await CaptureAsync(method))
            Assert.All(command.OuterProjection, expression => Assert.Matches(AllowedProjection, expression));
    }

    // The host is recognised in SQL, through the public profile: only its name and avatar come out.
    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(GroupMembers)]
    [InlineData(Desks)]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_JoinHostOnMemberProfiles_When_ReadingGroups(string method)
    {
        var commands = await CaptureAsync(method);

        Assert.Contains(commands, command => Regex.IsMatch(
            command.Text,
            @"FROM \[GroupSessions\] AS \[\w+\]\s*INNER JOIN \[MemberProfiles\] AS \[\w+\] ON \[\w+\]\.\[HostId\] = \[\w+\]\.\[UserId\]"));
    }

    // A sprint nobody joined is still read from PomodoroSessions, joined on its owner.
    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Desks)]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_JoinOwnerOnMemberProfiles_When_ReadingSoloSprints(string method)
    {
        var commands = await CaptureAsync(method);

        Assert.Contains(commands, command => Regex.IsMatch(
            command.Text,
            @"INNER JOIN \[MemberProfiles\] AS \[\w+\] ON \[\w+\]\.\[OwnerId\] = \[\w+\]\.\[UserId\]"));
    }

    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(GroupMembers)]
    [InlineData(Desks)]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_KeepDeadlineRulesOutOfSql_When_Queried(string method)
    {
        foreach (var command in await CaptureAsync(method))
        {
            Assert.DoesNotContain("DATEADD", command.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DATEDIFF", command.Text, StringComparison.OrdinalIgnoreCase);
        }
    }

    // A sprint nobody joined is open when not closed; a group, when not closed and not ended.
    [Theory]
    [InlineData(OpenGroups)]
    [InlineData(Joinable)]
    [InlineData(Desks)]
    public async Task Should_CheckClosure_When_ReadingOpenSprintsAndGroups(string method)
    {
        var commands = await CaptureAsync(method);

        Assert.Contains(commands, command =>
            command.Text.Contains("FROM [PomodoroSessions]", StringComparison.Ordinal)
            && command.Text.Contains("[IsClosedToJoin] = CAST(0 AS bit)", StringComparison.Ordinal));
        Assert.Contains(commands, command =>
            command.Text.Contains("FROM [GroupSessions]", StringComparison.Ordinal)
            && command.Text.Contains("[IsClosedToJoin] = CAST(0 AS bit)", StringComparison.Ordinal)
            && command.Text.Contains("[EndedAtUtc] IS NULL", StringComparison.Ordinal));
    }

    // Those already present stay visible to each other after the host closes the group, and once it ended.
    [Fact]
    public async Task Should_IgnoreClosureAndEnd_When_ReadingGroupMembers()
    {
        foreach (var command in await CaptureAsync(GroupMembers))
        {
            Assert.DoesNotContain("IsClosedToJoin", command.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("EndedAtUtc", command.Text, StringComparison.Ordinal);
        }
    }

    // A desk read by its root is returned closed to joins or not: only its end filters it.
    [Fact]
    public async Task Should_NotFilterOnClosure_When_ReadingDeskByRoot()
    {
        var commands = await CaptureAsync(Desk);

        Assert.Contains(commands, command =>
            command.Text.Contains("FROM [GroupSessions]", StringComparison.Ordinal)
            && command.Text.Contains("[EndedAtUtc] IS NULL", StringComparison.Ordinal));
        Assert.All(commands, command =>
            Assert.DoesNotContain("[IsClosedToJoin] = CAST(0 AS bit)", command.Text, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Desk)]
    [InlineData(HostAmbiance)]
    public async Task Should_FilterOnRoot_When_ReadingByRoot(string method)
    {
        var commands = await CaptureAsync(method);

        Assert.All(commands, command => Assert.Contains(command.Parameters, p => Equals(p.Value, Root)));
    }

    [Fact]
    public async Task Should_LookUpTheNameKey_When_ReadingJoinableSprint()
    {
        var commands = await CaptureAsync(Joinable);

        Assert.All(commands, command =>
        {
            Assert.Matches(@"\[\w+\]\.\[NameKey\] = @\w+", command.Text);
            Assert.Contains(command.Parameters, p => Equals(p.Value, "lea_dev"));
        });
    }

    private static Task<IReadOnlyList<CapturedCommand>> CaptureAsync(string method) =>
        SqlCommandCapture.CaptureAllAsync(context =>
        {
            var repository = new EfCoreOpenSprintReadRepository(context);
            return method switch
            {
                OpenGroups => repository.GetOpenGroupsAsync(Now, 10),
                Joinable => repository.FindJoinableAsync(ProfileName.Create("LEA_dev").Value, Now),
                GroupMembers => repository.GetGroupMembersAsync(Guid.NewGuid()),
                Desks => repository.GetDesksAsync(Now, 10),
                Desk => repository.GetDeskAsync(Root, Now),
                HostAmbiance => repository.GetHostAmbianceCodeAsync(Root),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        });
}
