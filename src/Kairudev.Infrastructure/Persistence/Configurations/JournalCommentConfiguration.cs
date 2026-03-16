using Kairudev.Domain.Journal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairudev.Infrastructure.Persistence.Configurations;

public sealed class JournalCommentConfiguration : IEntityTypeConfiguration<JournalComment>
{
    public void Configure(EntityTypeBuilder<JournalComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => JournalCommentId.From(value))
            .HasColumnType("nvarchar(36)");

        builder.Property(c => c.Text).IsRequired().HasMaxLength(1000);
    }
}
