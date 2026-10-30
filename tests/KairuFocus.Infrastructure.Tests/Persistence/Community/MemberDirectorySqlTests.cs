using System.Text.RegularExpressions;
using KairuFocus.Domain.Community;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// Spec §6.4 guard on the SQL of the cross-user member directory: never the Users table, an email or a
/// display name; a user identifier neither projected nor used beyond the join condition.
/// </summary>
public sealed class MemberDirectorySqlTests
{
    private const string FindByName = nameof(IMemberDirectoryReadRepository.FindByNameAsync);
    private const string GetActivity = nameof(IMemberDirectoryReadRepository.GetActivityAsync);

    private static readonly ProfileName Name = ProfileName.Create("Lea_Dev").Value;
    private static readonly DateTime Since = new(2025, 10, 25, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(FindByName)]
    [InlineData(GetActivity)]
    public async Task Should_NotReferenceUsersEmailOrDisplayName(string method)
    {
        var command = await CaptureAsync(method);

        Assert.DoesNotContain("[Users]", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Email", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DisplayName", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(FindByName)]
    [InlineData(GetActivity)]
    public async Task Should_NotProjectOwnerOrUserId(string method)
    {
        var command = await CaptureAsync(method);

        Assert.All(OuterSelectList(command.Text), expression =>
        {
            Assert.DoesNotContain("OwnerId", expression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UserId", expression, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Should_ProjectOnlyPublicColumns_When_FindingByName()
    {
        var command = await CaptureAsync(FindByName);

        Assert.Collection(
            OuterSelectList(command.Text),
            name => Assert.EndsWith(".[ProfileName]", name, StringComparison.Ordinal),
            source => Assert.EndsWith(".[NameSource]", source, StringComparison.Ordinal),
            avatar => Assert.EndsWith(".[AvatarUrl]", avatar, StringComparison.Ordinal),
            joinedAt => Assert.EndsWith(".[JoinedAtUtc]", joinedAt, StringComparison.Ordinal));
    }

    // The session identifier stays inside the "together" condition: it is never a selected column.
    [Fact]
    public async Task Should_ProjectOnlyInstantsAndTogetherFlag_When_GettingActivity()
    {
        var command = await CaptureAsync(GetActivity);

        Assert.Collection(
            OuterSelectList(command.Text),
            startedAt => Assert.EndsWith(".[StartedAt]", startedAt, StringComparison.Ordinal),
            endedAt => Assert.EndsWith(".[EndedAt]", endedAt, StringComparison.Ordinal),
            together =>
            {
                Assert.StartsWith("CASE", together, StringComparison.Ordinal);
                Assert.Contains("EXISTS", together, StringComparison.Ordinal);
            });
    }

    // Freezes the join between PomodoroSessions.OwnerId (nvarchar(50)) and MemberProfiles.UserId
    // (uniqueidentifier): a cast added by a provider or a mapping change would not go unnoticed. The two
    // identifiers appear nowhere else in the statement.
    [Fact]
    public async Task Should_JoinMemberProfilesOnOwner()
    {
        var command = await CaptureAsync(GetActivity);

        Assert.Matches(
            @"INNER JOIN \[MemberProfiles\] AS \[(\w+)\] ON \[\w+\]\.\[OwnerId\] = \[\1\]\.\[UserId\]",
            command.Text);
        Assert.Single(Regex.Matches(command.Text, "OwnerId"));
        Assert.Single(Regex.Matches(command.Text, "UserId"));
    }

    [Theory]
    [InlineData(FindByName)]
    [InlineData(GetActivity)]
    public async Task Should_FilterOnNormalizedKey(string method)
    {
        var command = await CaptureAsync(method);

        Assert.Matches(@"\[\w+\]\.\[NameKey\] = @", command.Text);
        Assert.Contains(command.Parameters, p => Equals(p.Value, Name.Key));
    }

    // Per-row rules stay in CommunityRules (ADR-024).
    [Fact]
    public async Task Should_KeepRowRulesOutOfSql_When_GettingActivity()
    {
        var command = await CaptureAsync(GetActivity);

        Assert.DoesNotContain("DATEADD", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DATEDIFF", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<CapturedCommand> CaptureAsync(string method) =>
        SqlCommandCapture.CaptureAsync(context =>
        {
            var repository = new EfCoreMemberDirectoryReadRepository(context);
            return method switch
            {
                FindByName => repository.FindByNameAsync(Name),
                GetActivity => repository.GetActivityAsync(Name, Since),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        });

    /// <summary>
    /// Expressions of the outermost SELECT list. Unlike <see cref="CapturedCommand.OuterProjection"/>,
    /// supports a subquery in the select list: the list ends at the first FROM outside any parenthesis.
    /// </summary>
    private static List<string> OuterSelectList(string sql)
    {
        const string select = "SELECT ";
        var listStart = sql.IndexOf(select, StringComparison.Ordinal) + select.Length;

        var expressions = new List<string>();
        var depth = 0;
        var start = listStart;
        for (var i = listStart; i < sql.Length; i++)
        {
            switch (sql[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    expressions.Add(sql[start..i].Trim());
                    start = i + 1;
                    break;
                case 'F' when depth == 0 && string.CompareOrdinal(sql, i, "FROM", 0, 4) == 0:
                    expressions.Add(sql[start..i].Trim());
                    return expressions;
            }
        }

        Assert.Fail($"No top-level FROM found in: {sql}");
        return expressions;
    }
}
