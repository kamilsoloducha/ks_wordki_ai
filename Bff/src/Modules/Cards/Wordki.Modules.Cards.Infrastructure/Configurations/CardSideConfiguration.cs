using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Infrastructure.Configurations;

public sealed class CardSideConfiguration : IEntityTypeConfiguration<CardSide>
{
    public void Configure(EntityTypeBuilder<CardSide> builder)
    {
        builder.ToTable("card_sides", "cards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Label)
            .HasColumnName("label")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Example)
            .HasColumnName("example")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000)
            .IsRequired();
    }
}
