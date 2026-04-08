using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Infrastructure.Configurations;

public sealed class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("results", "cards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(x => x.CardSideId)
            .HasColumnName("card_side_id")
            .IsRequired();

        builder.Property(x => x.Drawer)
            .HasColumnName("drawer")
            .IsRequired();

        builder.Property(x => x.NextRepeatUtc)
            .HasColumnName("next_repeat_utc");

        builder.Property(x => x.Counter)
            .HasColumnName("counter")
            .IsRequired();

        builder.Property(x => x.IsTicked)
            .HasColumnName("is_ticked")
            .IsRequired();

        builder.HasOne(x => x.CardSide)
            .WithOne(x => x.Result)
            .HasForeignKey<Result>(x => x.CardSideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.CardSideId).IsUnique();
    }
}
