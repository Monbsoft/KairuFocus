using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KairuFocus.Infrastructure.Tests.Persistence.Pomodoro;

/// <summary>
/// Reads the design-time model (no database): the shape the shared sprint columns of the
/// community profiles migration must produce.
/// </summary>
public sealed class PomodoroSessionConfigurationTests
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_MapSharedSprintIdAsNullableGuid_When_ModelIsBuilt()
    {
        var property = SessionEntityType().FindProperty(nameof(PomodoroSession.SharedSprintId))!;

        Assert.Equal("SharedSprintId", property.GetColumnName());
        Assert.Equal("uniqueidentifier", property.GetColumnType());
        Assert.True(property.IsNullable);
        Assert.Equal(typeof(Guid), property.GetTypeMapping().Converter!.ProviderClrType);
    }

    [Fact]
    public void Should_IndexSharedSprintIdWithoutForeignKey_When_ModelIsBuilt()
    {
        var session = SessionEntityType();
        var sharedSprintId = session.FindProperty(nameof(PomodoroSession.SharedSprintId))!;

        var index = Assert.Single(session.GetIndexes(), i => i.Properties.SequenceEqual([sharedSprintId]));
        Assert.Equal("IX_PomodoroSessions_SharedSprintId", index.GetDatabaseName());
        Assert.False(index.IsUnique);
        Assert.Null(index.GetFilter());
        Assert.DoesNotContain(session.GetForeignKeys(), f => f.Properties.Contains(sharedSprintId));
    }

    [Fact]
    public void Should_MapSharedEndAtAsNullableDatetime2_When_ModelIsBuilt()
    {
        var property = SessionEntityType().FindProperty(nameof(PomodoroSession.SharedEndAt))!;

        Assert.Equal("datetime2", property.GetColumnType());
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Should_MapIsClosedToJoinAsBitDefaultingToFalse_When_ModelIsBuilt()
    {
        var property = SessionEntityType().FindProperty(nameof(PomodoroSession.IsClosedToJoin))!;

        Assert.Equal("bit", property.GetColumnType());
        Assert.False(property.IsNullable);
        Assert.Equal(false, property.GetDefaultValue());
    }

    [Fact]
    public async Task Should_RestoreSharedSprintFields_When_JoinedSessionIsReadBack()
    {
        var database = new InMemoryDatabase();
        var rootId = PomodoroSessionId.New();
        var sharedEndAt = Now.AddMinutes(20);
        var joined = PomodoroSession.CreateJoined(UserId.New(), rootId, sharedEndAt, Now).Value;
        await database.SeedAsync(joined);

        await using var context = database.CreateContext();
        var reloaded = await context.Set<PomodoroSession>().SingleAsync(s => s.Id == joined.Id);

        Assert.Equal(rootId, reloaded.SharedSprintId);
        Assert.Equal(sharedEndAt, reloaded.SharedEndAt);
        Assert.False(reloaded.IsClosedToJoin);
    }

    [Fact]
    public async Task Should_ReadNullSharedSprintId_When_SessionIsSolo()
    {
        var database = new InMemoryDatabase();
        var solo = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, UserId.New());
        await database.SeedAsync(solo);

        await using var context = database.CreateContext();
        var reloaded = await context.Set<PomodoroSession>().SingleAsync(s => s.Id == solo.Id);

        Assert.Null(reloaded.SharedSprintId);
        Assert.Null(reloaded.SharedEndAt);
    }

    [Fact]
    public async Task Should_RestoreClosedFlag_When_RootSprintIsClosedToJoin()
    {
        var database = new InMemoryDatabase();
        var root = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, UserId.New());
        root.Start(Now);
        root.CloseToJoin();
        await database.SeedAsync(root);

        await using var context = database.CreateContext();
        var reloaded = await context.Set<PomodoroSession>().SingleAsync(s => s.Id == root.Id);

        Assert.True(reloaded.IsClosedToJoin);
    }

    private static IEntityType SessionEntityType()
    {
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .Options;
        using var context = new KairuFocusDbContext(options);

        var model = context.GetService<IDesignTimeModel>().Model;
        return model.FindEntityType(typeof(PomodoroSession))!;
    }
}
