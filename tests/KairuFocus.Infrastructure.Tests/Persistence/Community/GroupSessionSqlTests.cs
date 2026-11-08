using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>SQL Server shape of the group session writes (no database: the command is captured).</summary>
public sealed class GroupSessionSqlTests
{
    private static readonly DateTime StartedAtUtc = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_FilterOnVersion_When_Updating()
    {
        var group = GroupSession.Start(
            Guid.NewGuid(), UserId.New(), StartedAtUtc, StartedAtUtc.AddMinutes(25), UserId.New(), StartedAtUtc.AddMinutes(5)).Value;

        var command = await SqlCommandCapture.CaptureAsync(async context =>
        {
            // Tracked as loaded from the database (Version = 0), then modified through the domain.
            context.Attach(group);
            Assert.True(group.Stop(GroupSessionEndReason.Stopped, StartedAtUtc.AddMinutes(6)).IsSuccess);
            await new EfCoreGroupSessionRepository(context).TryUpdateAsync(group);
        });

        Assert.Contains("UPDATE [GroupSessions]", command.Text);
        Assert.Contains("[Version] = @", command.Text);
    }
}
