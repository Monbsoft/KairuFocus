using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KairuFocus.Infrastructure.Persistence;

internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .ValueGeneratedNever();

        builder.Property(s => s.ThemePreference)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        // RingtonePreference.None and a volume of 0 are valid user choices, but they are also the CLR defaults,
        // which EF treats as "not set" for a column with a store default: it would omit the column on INSERT and
        // let the database write its own default instead. An out-of-range sentinel makes EF always send the value.
        builder.Property(s => s.RingtonePreference)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(RingtonePreference.AlarmClock)
            .HasSentinel((RingtonePreference)(-1));

        builder.Property(s => s.AlarmVolume)
            .HasDefaultValue(80)
            .HasSentinel(-1)
            .IsRequired();

        builder.Property(s => s.JiraBaseUrl).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.Property(s => s.JiraEmail).HasColumnType("nvarchar(200)").HasMaxLength(200);
        builder.Property(s => s.JiraApiToken).HasColumnType("nvarchar(500)").HasMaxLength(500);
    }
}
