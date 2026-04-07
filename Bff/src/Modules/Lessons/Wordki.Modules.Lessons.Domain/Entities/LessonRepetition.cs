namespace Wordki.Modules.Lessons.Domain.Entities;

/// <summary>
/// Pojedyncze powtórzenie w ramach lekcji.
/// <see cref="QuestionResultId"/> — id wiersza <c>cards.results</c> dla strony pytania (SRS).
/// </summary>
public sealed class LessonRepetition
{
    public long Id { get; init; }
    public long LessonId { get; init; }
    public Lesson Lesson { get; init; } = null!;
    /// <summary>Kolejność w ramach lekcji (1, 2, …).</summary>
    public int SequenceNumber { get; init; }
    /// <summary>Id wyniku (<c>cards.results.id</c>) przypisanego do strony pytania.</summary>
    public long QuestionResultId { get; init; }
    /// <summary>Czy użytkownik znał odpowiedź (wynik oceny).</summary>
    public bool IsKnown { get; init; }
    /// <summary>Czas zapisu odpowiedzi (UTC).</summary>
    public DateTime AnsweredAtUtc { get; init; }
}
