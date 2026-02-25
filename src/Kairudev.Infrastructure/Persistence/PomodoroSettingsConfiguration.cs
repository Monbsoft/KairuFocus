using Kairudev.Infrastructure.Persistence.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairudev.Infrastructure.Persistence;

internal sealed class PomodoroSettingsConfiguration : IEntityTypeConfiguration<PomodoroSettingsRow>
{
    public void Configure(EntityTypeBuilder<PomodoroSettingsRow> builder)
    {
        builder.ToTable("PomodoroSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.SprintDurationMinutes).IsRequired();
        builder.Property(s => s.ShortBreakDurationMinutes).IsRequired();
        builder.Property(s => s.LongBreakDurationMinutes).IsRequired();
    }
}
