using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

/// <summary>
/// Reads the design-time model (no database): the shape the AddLeaderboardProfiles migration must produce.
/// </summary>
public sealed class LeaderboardProfileConfigurationTests
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    [Fact]
    public void Should_KeyProfilesByUserIdColumn_When_ModelIsBuilt()
    {
        var profile = ProfileEntityType();

        Assert.Equal("LeaderboardProfiles", profile.GetTableName());
        var primaryKey = profile.FindPrimaryKey()!;
        Assert.Equal("PK_LeaderboardProfiles", primaryKey.GetName());
        var key = Assert.Single(primaryKey.Properties);
        Assert.Equal("UserId", key.GetColumnName());
        Assert.Equal("uniqueidentifier", key.GetColumnType());
        Assert.Equal(ValueGenerated.Never, key.ValueGenerated);
    }

    [Fact]
    public void Should_DeclareUniquePseudonymIndex_When_ModelIsBuilt()
    {
        var profile = ProfileEntityType();

        var pseudonym = profile.FindProperty(nameof(LeaderboardProfile.Pseudonym))!;
        Assert.Equal("nvarchar(60)", pseudonym.GetColumnType());
        Assert.False(pseudonym.IsNullable);
        var index = Assert.Single(profile.GetIndexes(), i => i.Properties.SequenceEqual([pseudonym]));
        Assert.True(index.IsUnique);
        Assert.Equal("IX_LeaderboardProfiles_Pseudonym", index.GetDatabaseName());
        Assert.Null(index.GetFilter());
    }

    [Fact]
    public void Should_CascadeProfileDeletion_When_UserIsDeleted()
    {
        var profile = ProfileEntityType();

        var foreignKey = Assert.Single(profile.GetForeignKeys());
        Assert.Equal(typeof(User), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.True(foreignKey.IsUnique);
        Assert.Equal("UserId", Assert.Single(foreignKey.Properties).GetColumnName());
        Assert.Equal("FK_LeaderboardProfiles_Users_UserId", foreignKey.GetConstraintName());
    }

    private static IEntityType ProfileEntityType()
    {
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .Options;
        using var context = new KairuFocusDbContext(options);

        // The design-time model keeps the relational annotations (column types, constraint names).
        var model = context.GetService<IDesignTimeModel>().Model;
        return model.FindEntityType(typeof(LeaderboardProfile))!;
    }
}
