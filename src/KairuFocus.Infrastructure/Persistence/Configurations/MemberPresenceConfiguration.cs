using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KairuFocus.Infrastructure.Persistence.Configurations;

public sealed class MemberPresenceConfiguration : IEntityTypeConfiguration<MemberPresence>
{
    // Name of the primary key: the repository matches it against the SQL Server duplicate-key message to
    // tell two first signals of the same member racing from any other failure. (Migrations keep their own
    // literal: a migration is a frozen snapshot of the schema at the time it was written.)
    internal const string PrimaryKeyName = "PK_MemberPresences";

    // Name of the foreign key to the profile, the one EF Core gives by convention (checked by a model test): the
    // repository matches it to tell an insert whose profile is gone (member left the community) from any other failure.
    internal const string ProfileForeignKeyName = "FK_MemberPresences_MemberProfiles_UserId";

    public void Configure(EntityTypeBuilder<MemberPresence> builder)
    {
        builder.ToTable("MemberPresences");

        // One presence per member: the member's id is the key.
        builder.HasKey(p => p.Id)
            .HasName(PrimaryKeyName);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("UserId")
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(p => p.LastSeenAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        // The "who is connected" read filters on the last sighting.
        builder.HasIndex(p => p.LastSeenAtUtc)
            .HasDatabaseName("IX_MemberPresences_LastSeenAtUtc");

        // Leaving the community deletes the profile, and the presence with it.
        builder.HasOne<MemberProfile>()
            .WithOne()
            .HasForeignKey<MemberPresence>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
