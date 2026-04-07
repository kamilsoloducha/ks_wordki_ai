using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Infrastructure.Configurations;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards", "cards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(x => x.FrontSideId)
            .HasColumnName("front_side_id")
            .IsRequired();

        builder.Property(x => x.BackSideId)
            .HasColumnName("back_side_id")
            .IsRequired();

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Cards)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FrontSide)
            .WithOne(x => x.CardWhereFront)
            .HasForeignKey<Card>(x => x.FrontSideId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BackSide)
            .WithOne(x => x.CardWhereBack)
            .HasForeignKey<Card>(x => x.BackSideId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.FrontSideId).IsUnique();
        builder.HasIndex(x => x.BackSideId).IsUnique();
    }
}
