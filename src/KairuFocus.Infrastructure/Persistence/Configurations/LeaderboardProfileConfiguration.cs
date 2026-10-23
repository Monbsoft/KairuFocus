using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KairuFocus.Infrastructure.Persistence.Configurations;

public sealed class LeaderboardProfileConfiguration : IEntityTypeConfiguration<LeaderboardProfile>
{
    // Names of the two uniqueness constraints of the table. Single source of truth: the repository
    // matches them against the SQL Server duplicate-key message to tell a uniqueness conflict from any
    // other failure, so a rename here must reach that check too. (Migrations keep their own literal:
    // a migration is a frozen snapshot of the schema at the time it was written.)
    internal const string PrimaryKeyName = "PK_LeaderboardProfiles";
    internal const string PseudonymIndexName = "IX_LeaderboardProfiles_Pseudonym";

    public void Configure(EntityTypeBuilder<LeaderboardProfile> builder)
    {
        builder.ToTable("LeaderboardProfiles");

        // One profile per user: the user's id is the key (same pattern as UserSettings).
        builder.HasKey(p => p.Id)
            .HasName(PrimaryKeyName);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("UserId")
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(p => p.Pseudonym)
            .HasConversion(p => p.Value, v => Pseudonym.Restore(v))
            .HasColumnType("nvarchar(60)")
            .HasMaxLength(Pseudonym.MaxLength)
            .IsRequired();

        // The pseudonym is the only public name of a participant and the key the Application uses
        // to find "my row" in the standings (ADR-024): it must be unique.
        builder.HasIndex(p => p.Pseudonym)
            .IsUnique()
            .HasDatabaseName(PseudonymIndexName);

        builder.Property(p => p.JoinedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<LeaderboardProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
