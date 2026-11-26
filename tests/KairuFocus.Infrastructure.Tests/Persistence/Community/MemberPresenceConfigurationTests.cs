using KairuFocus.Domain.Community;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// Reads the design-time model (no database): the shape the open space migration must produce.
/// </summary>
public sealed class MemberPresenceConfigurationTests
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    [Fact]
    public void Should_KeyPresenceByMemberId_When_ModelIsBuilt()
    {
        var presence = Model().FindEntityType(typeof(MemberPresence))!;

        Assert.Equal("MemberPresences", presence.GetTableName());
        var primaryKey = presence.FindPrimaryKey()!;
        Assert.Equal("PK_MemberPresences", primaryKey.GetName());
        var key = Assert.Single(primaryKey.Properties);
        Assert.Equal("UserId", key.GetColumnName());
        Assert.Equal("uniqueidentifier", key.GetColumnType());
        Assert.Equal(ValueGenerated.Never, key.ValueGenerated);

        var lastSeen = presence.FindProperty(nameof(MemberPresence.LastSeenAtUtc))!;
        Assert.Equal("datetime2", lastSeen.GetColumnType());
        Assert.False(lastSeen.IsNullable);
    }

    [Fact]
    public void Should_IndexLastSeenAndCascadeFromProfile_When_ModelIsBuilt()
    {
        var presence = Model().FindEntityType(typeof(MemberPresence))!;

        var lastSeen = presence.FindProperty(nameof(MemberPresence.LastSeenAtUtc))!;
        var index = Assert.Single(presence.GetIndexes(), i => i.Properties.SequenceEqual([lastSeen]));
        Assert.False(index.IsUnique);
        Assert.Equal("IX_MemberPresences_LastSeenAtUtc", index.GetDatabaseName());

        var foreignKey = Assert.Single(presence.GetForeignKeys());
        Assert.Equal(typeof(MemberProfile), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.True(foreignKey.IsUnique);
        Assert.Equal("UserId", Assert.Single(foreignKey.Properties).GetColumnName());
    }

    [Fact]
    public void Should_MapConsentAndAmbianceAsNullableWithoutDefault_When_ModelIsBuilt()
    {
        var profile = Model().FindEntityType(typeof(MemberProfile))!;

        var consent = profile.FindProperty(nameof(MemberProfile.PresenceConsentedAtUtc))!;
        Assert.Equal("datetime2", consent.GetColumnType());
        Assert.True(consent.IsNullable);
        Assert.Null(consent.GetDefaultValue());
        Assert.Null(consent.GetDefaultValueSql());

        var ambiance = profile.FindProperty(nameof(MemberProfile.AmbianceCode))!;
        Assert.Equal("nvarchar(40)", ambiance.GetColumnType());
        Assert.Equal(AmbianceCatalogue.MaxCodeLength, ambiance.GetMaxLength());
        Assert.True(ambiance.IsNullable);
        Assert.Null(ambiance.GetDefaultValue());
        Assert.Null(ambiance.GetDefaultValueSql());
    }

    private static IModel Model()
    {
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .Options;
        using var context = new KairuFocusDbContext(options);

        // The design-time model keeps the relational annotations (column types, constraint names).
        return context.GetService<IDesignTimeModel>().Model;
    }
}
