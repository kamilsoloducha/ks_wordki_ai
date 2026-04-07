using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Modules.Lessons.Domain.Entities;

namespace Wordki.Modules.Lessons.Infrastructure.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons", "lessons");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.LessonKind)
            .HasColumnName("lesson_kind")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.WordCount)
            .HasColumnName("word_count");

        builder.Property(x => x.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.HasIndex(x => x.UserId);

        builder.HasMany(x => x.Repetitions)
            .WithOne(x => x.Lesson)
            .HasForeignKey(x => x.LessonId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
