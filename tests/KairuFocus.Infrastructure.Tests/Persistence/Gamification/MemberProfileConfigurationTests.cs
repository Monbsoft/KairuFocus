using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KairuFocus.Infrastructure.Tests.Persistence.Gamification;

/// <summary>
/// Reads the design-time model (no database): the shape the member profiles migration must produce.
/// </summary>
public sealed class MemberProfileConfigurationTests
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    [Fact]
    public void Should_KeyProfilesByUserIdColumn_When_ModelIsBuilt()
    {
        var profile = ProfileEntityType();

        Assert.Equal("MemberProfiles", profile.GetTableName());
        var primaryKey = profile.FindPrimaryKey()!;
        Assert.Equal("PK_MemberProfiles", primaryKey.GetName());
        var key = Assert.Single(primaryKey.Properties);
        Assert.Equal("UserId", key.GetColumnName());
        Assert.Equal("uniqueidentifier", key.GetColumnType());
        Assert.Equal(ValueGenerated.Never, key.ValueGenerated);
    }

    [Fact]
    public void Should_StoreNameInProfileNameColumn_When_ModelIsBuilt()
    {
        var name = ProfileEntityType().FindProperty(nameof(MemberProfile.Name))!;

        Assert.Equal("ProfileName", name.GetColumnName());
        Assert.Equal("nvarchar(39)", name.GetColumnType());
        Assert.False(name.IsNullable);
    }

    [Fact]
    public void Should_DeclareUniqueNameKeyIndex_When_ModelIsBuilt()
    {
        var profile = ProfileEntityType();

        var nameKey = profile.FindProperty(nameof(MemberProfile.NameKey))!;
        Assert.Equal("nvarchar(39)", nameKey.GetColumnType());
        Assert.False(nameKey.IsNullable);
        var index = Assert.Single(profile.GetIndexes(), i => i.Properties.SequenceEqual([nameKey]));
        Assert.True(index.IsUnique);
        Assert.Equal("IX_MemberProfiles_NameKey", index.GetDatabaseName());
        Assert.Null(index.GetFilter());
    }

    [Fact]
    public void Should_MapSourceAvatarAndJoinDate_When_ModelIsBuilt()
    {
        var profile = ProfileEntityType();

        var source = profile.FindProperty(nameof(MemberProfile.NameSource))!;
        Assert.Equal("nvarchar(20)", source.GetColumnType());
        Assert.Equal(typeof(string), source.GetProviderClrType());
        Assert.False(source.IsNullable);

        var avatar = profile.FindProperty(nameof(MemberProfile.AvatarUrl))!;
        Assert.Equal("nvarchar(500)", avatar.GetColumnType());
        Assert.True(avatar.IsNullable);

        var joinedAt = profile.FindProperty(nameof(MemberProfile.JoinedAtUtc))!;
        Assert.Equal("datetime2", joinedAt.GetColumnType());
        Assert.False(joinedAt.IsNullable);
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
        Assert.Equal("FK_MemberProfiles_Users_UserId", foreignKey.GetConstraintName());
    }

    private static IEntityType ProfileEntityType()
    {
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .Options;
        using var context = new KairuFocusDbContext(options);

        // The design-time model keeps the relational annotations (column types, constraint names).
        var model = context.GetService<IDesignTimeModel>().Model;
        return model.FindEntityType(typeof(MemberProfile))!;
    }
}
