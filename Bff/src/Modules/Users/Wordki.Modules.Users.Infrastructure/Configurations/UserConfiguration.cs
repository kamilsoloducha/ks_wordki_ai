using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.NormalizedEmail)
            .HasConversion(
                normalizedEmail => normalizedEmail.Value,
                rawValue => NormalizedEmail.Create(rawValue))
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique();

        builder.Property(x => x.UserName)
            .HasColumnName("user_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EmailConfirmationTokenHash)
            .HasMaxLength(512);

        builder.Property(x => x.SecurityStamp)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DeletedAtUtc);

        builder.HasQueryFilter(x => x.Status != UserStatus.Deleted);
    }
}
