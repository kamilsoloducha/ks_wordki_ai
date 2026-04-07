using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordki.Modules.Lessons.Domain.Entities;

namespace Wordki.Modules.Lessons.Infrastructure.Configurations;

public sealed class LessonRepetitionConfiguration : IEntityTypeConfiguration<LessonRepetition>
{
    public void Configure(EntityTypeBuilder<LessonRepetition> builder)
    {
        builder.ToTable("lesson_repetitions", "lessons");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.LessonId)
            .HasColumnName("lesson_id");

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.Property(x => x.QuestionResultId)
            .HasColumnName("question_result_id");

        builder.Property(x => x.IsKnown)
            .HasColumnName("is_known");

        builder.Property(x => x.AnsweredAtUtc)
            .HasColumnName("answered_at_utc")
            .IsRequired();

        builder.HasIndex(x => x.LessonId);
        builder.HasIndex(x => x.QuestionResultId);

        builder.HasIndex(x => new { x.LessonId, x.SequenceNumber })
            .IsUnique();
    }
}
