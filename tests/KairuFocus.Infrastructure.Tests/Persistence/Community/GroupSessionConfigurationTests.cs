using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence;
using KairuFocus.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// Reads the design-time model (no database): the shape the AddGroupSessions migration must produce.
/// </summary>
public sealed class GroupSessionConfigurationTests
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    [Fact]
    public void Should_KeyGroupsByAssignedId_When_ModelIsBuilt()
    {
        var group = Entity<GroupSession>();

        Assert.Equal("GroupSessions", group.GetTableName());
        var primaryKey = group.FindPrimaryKey()!;
        Assert.Equal("PK_GroupSessions", primaryKey.GetName());
        var key = Assert.Single(primaryKey.Properties);
        Assert.Equal("Id", key.GetColumnName());
        Assert.Equal("uniqueidentifier", key.GetColumnType());
        Assert.Equal(ValueGenerated.Never, key.ValueGenerated);
    }

    [Fact]
    public void Should_MapHostAndCascadeFromUsers_When_ModelIsBuilt()
    {
        var group = Entity<GroupSession>();

        var host = group.FindProperty("_hostId")!;
        Assert.Equal("HostId", host.GetColumnName());
        Assert.Equal("uniqueidentifier", host.GetColumnType());
        Assert.False(host.IsNullable);

        var foreignKey = Assert.Single(group.GetForeignKeys());
        Assert.Equal(typeof(User), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);

        var index = Assert.Single(group.GetIndexes());
        Assert.Equal("IX_GroupSessions_HostId", index.GetDatabaseName());
        Assert.Equal([host], index.Properties);
    }

    [Fact]
    public void Should_StoreCurrentStepInGroupRow_When_ModelIsBuilt()
    {
        var group = Entity<GroupSession>();
        var step = group.FindNavigation(nameof(GroupSession.CurrentStep))!.TargetEntityType;

        Assert.Equal("CurrentStepNumber", step.FindProperty(nameof(GroupStep.Number))!.GetColumnName());
        var type = step.FindProperty(nameof(GroupStep.Type))!;
        Assert.Equal("CurrentStepType", type.GetColumnName());
        Assert.Equal("nvarchar(20)", type.GetColumnType());
        Assert.Equal("CurrentStepStartedAtUtc", step.FindProperty(nameof(GroupStep.StartedAtUtc))!.GetColumnName());
        Assert.Equal("CurrentStepEndsAtUtc", step.FindProperty(nameof(GroupStep.EndsAtUtc))!.GetColumnName());
        Assert.Equal("GroupSessions", step.GetTableName());
    }

    [Fact]
    public void Should_MapEndAndConcurrencyColumns_When_ModelIsBuilt()
    {
        var group = Entity<GroupSession>();

        var endReason = group.FindProperty(nameof(GroupSession.EndReason))!;
        Assert.Equal("nvarchar(30)", endReason.GetColumnType());
        Assert.True(endReason.IsNullable);
        Assert.True(group.FindProperty(nameof(GroupSession.EndedAtUtc))!.IsNullable);
        Assert.Equal("bit", group.FindProperty(nameof(GroupSession.IsClosedToJoin))!.GetColumnType());
        Assert.Equal(false, group.FindProperty(nameof(GroupSession.IsClosedToJoin))!.GetDefaultValue());
        var version = group.FindProperty(nameof(GroupSession.Version))!;
        Assert.Equal("int", version.GetColumnType());
        Assert.True(version.IsConcurrencyToken);
    }

    [Fact]
    public void Should_KeyMembersByGroupAndUser_When_ModelIsBuilt()
    {
        var member = Entity<GroupMember>();

        Assert.Equal("GroupSessionMembers", member.GetTableName());
        var primaryKey = member.FindPrimaryKey()!;
        Assert.Equal(["GroupSessionId", "UserId"], primaryKey.Properties.Select(p => p.GetColumnName()));
        // The repository recognises a duplicate membership by this name in the SQL Server error.
        Assert.Equal(GroupMemberConfiguration.PrimaryKeyName, primaryKey.GetName());
        Assert.Equal("uniqueidentifier", member.FindProperty("_userId")!.GetColumnType());
        Assert.True(member.FindProperty(nameof(GroupMember.LeftAtUtc))!.IsNullable);
    }

    [Fact]
    public void Should_CascadeFromGroupOnly_When_ModelIsBuilt()
    {
        var member = Entity<GroupMember>();

        // No foreign key to Users: a second cascade path is refused by SQL Server.
        var foreignKey = Assert.Single(member.GetForeignKeys());
        Assert.Equal(typeof(GroupSession), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);

        var index = Assert.Single(member.GetIndexes());
        Assert.Equal("IX_GroupSessionMembers_UserId", index.GetDatabaseName());
        Assert.Equal("UserId", Assert.Single(index.Properties).GetColumnName());
    }

    private static IEntityType Entity<T>()
    {
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .Options;
        using var context = new KairuFocusDbContext(options);

        // The design-time model keeps the relational annotations (column types, constraint names).
        var model = context.GetService<IDesignTimeModel>().Model;
        return model.FindEntityType(typeof(T))!;
    }
}
