using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KairuFocus.Infrastructure.Persistence;

internal sealed class PomodoroSessionConfiguration : IEntityTypeConfiguration<PomodoroSession>
{
    /// <summary>
    /// Name of the filtered unique index enforcing "at most one Active session per owner".
    /// Single source of truth: the repository matches it against the SQL Server duplicate-key
    /// message to tell a concurrent start from any other constraint violation, so a rename here
    /// must reach that check too. (Existing migrations keep their own literal on purpose:
    /// a migration is a frozen snapshot of the schema at the time it was written.)
    /// </summary>
    internal const string ActiveSessionIndexName = "IX_PomodoroSessions_OwnerId_ActiveUnique";

    public void Configure(EntityTypeBuilder<PomodoroSession> builder)
    {
        builder.ToTable("PomodoroSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(
                id => id.Value,
                value => PomodoroSessionId.From(value))
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();

        builder.Property(s => s.OwnerId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        // nvarchar(20), not nvarchar(max): SQL Server forbids LOB columns in the predicate
        // of a filtered index, and the Status column is used by the unique index below.
        // Longest values: "Interrupted" (11) and "ShortBreak" (10).
        builder.Property(s => s.SessionType)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.PlannedDurationMinutes).HasColumnType("int").IsRequired();
        builder.Property(s => s.StartedAt).HasColumnType("datetime2");
        builder.Property(s => s.EndedAt).HasColumnType("datetime2");

        // Shared sprint (iteration #44). SharedSprintId has no setter: it is read and written through
        // the private _sharedSprintId field, found by convention. No foreign key on purpose: the root
        // session may be deleted on its own, and a joined session then keeps a dangling reference
        // that the read side simply ignores.
        builder.Property(s => s.SharedSprintId)
            .HasConversion(
                id => id!.Value,
                value => PomodoroSessionId.From(value))
            .HasColumnType("uniqueidentifier")
            .IsRequired(false);

        builder.HasIndex(s => s.SharedSprintId)
            .HasDatabaseName("IX_PomodoroSessions_SharedSprintId");

        builder.Property(s => s.SharedEndAt).HasColumnType("datetime2");

        builder.Property(s => s.IsClosedToJoin)
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(s => s.JournalComment)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        // Ignore the domain-facing property (TaskId is not an EF entity)
        builder.Ignore(s => s.LinkedTaskIds);

        // Stores the linked task IDs as a JSON array of GUIDs (EF Core 8+ primitive collection)
        builder.PrimitiveCollection(s => s.LinkedTaskIdValues)
            .HasColumnName("LinkedTaskIds")
            .HasColumnType("nvarchar(max)");

        // At most one Active session per user — enforced by the database, because the
        // "read then insert" done by StartSession cannot be atomic on its own.
        // "AND [OwnerId] IS NOT NULL" is required: OwnerId is optional and SQL Server treats
        // NULLs as equal in a unique index, so legacy NULL-owner rows would break the index.
        builder.HasIndex(s => s.OwnerId)
            .IsUnique()
            .HasDatabaseName(ActiveSessionIndexName)
            // N'Active' (Unicode literal): Status is nvarchar, a non-Unicode literal would force
            // a CONVERT_IMPLICIT and stop the optimiser from matching the queries EF emits.
            .HasFilter("[Status] = N'Active' AND [OwnerId] IS NOT NULL");
    }
}
