using Kairu.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairu.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(v => v.Value, v => UserId.From(v));

        builder.Property(u => u.GitHubId).HasColumnType("nvarchar(50)").HasMaxLength(50).IsRequired();
        builder.HasIndex(u => u.GitHubId).IsUnique();

        builder.Property(u => u.Login).HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(u => u.DisplayName).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasColumnType("nvarchar(200)").HasMaxLength(200);
    }
}
