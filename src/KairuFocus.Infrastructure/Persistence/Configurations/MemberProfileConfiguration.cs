using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KairuFocus.Infrastructure.Persistence.Configurations;

public sealed class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    // Names of the two uniqueness constraints of the table. Single source of truth: the repository
    // matches them against the SQL Server duplicate-key message to tell a uniqueness conflict from any
    // other failure, so a rename here must reach that check too. (Migrations keep their own literal:
    // a migration is a frozen snapshot of the schema at the time it was written.)
    internal const string PrimaryKeyName = "PK_MemberProfiles";
    internal const string NameKeyIndexName = "IX_MemberProfiles_NameKey";

    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        builder.ToTable("MemberProfiles");

        // One profile per user: the user's id is the key (same pattern as UserSettings).
        builder.HasKey(p => p.Id)
            .HasName(PrimaryKeyName);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("UserId")
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(p => p.Name)
            .HasConversion(n => n.Value, v => ProfileName.Restore(v))
            .HasColumnName("ProfileName")
            .HasColumnType("nvarchar(39)")
            .HasMaxLength(ProfileName.MaxLength)
            .IsRequired();

        // Case-insensitive identity of the name, stored apart so that uniqueness does not depend on the
        // database collation. The public name of a member is the key the Application uses to find
        // "my row" in the standings (ADR-024): it must be unique.
        builder.Property(p => p.NameKey)
            .HasColumnType("nvarchar(39)")
            .HasMaxLength(ProfileName.MaxLength)
            .IsRequired();

        builder.HasIndex(p => p.NameKey)
            .IsUnique()
            .HasDatabaseName(NameKeyIndexName);

        builder.Property(p => p.NameSource)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.AvatarUrl)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500);

        builder.Property(p => p.JoinedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<MemberProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
