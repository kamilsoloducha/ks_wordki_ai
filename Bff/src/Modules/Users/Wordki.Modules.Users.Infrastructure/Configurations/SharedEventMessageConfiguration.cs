using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Bff.SharedKernel.Events;

namespace Wordki.Modules.Users.Infrastructure.Configurations;

public sealed class SharedEventMessageConfiguration : IEntityTypeConfiguration<SharedEventMessage>
{
    public void Configure(EntityTypeBuilder<SharedEventMessage> builder)
    {
        builder.ToTable("shared_event_messages", "users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PublisherName)
            .HasColumnName("publisher_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ConsumerName)
            .HasColumnName("consumer_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.DataType)
            .HasColumnName("data_type")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AddedAtUtc)
            .HasColumnName("added_at_utc")
            .IsRequired();

        builder.Property(x => x.HandledAtUtc)
            .HasColumnName("handled_at_utc");

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.HasIndex(x => x.AddedAtUtc);
        builder.HasIndex(x => x.HandledAtUtc);
    }
}
