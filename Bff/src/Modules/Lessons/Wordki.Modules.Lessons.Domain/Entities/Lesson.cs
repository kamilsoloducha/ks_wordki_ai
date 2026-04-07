namespace Wordki.Modules.Lessons.Domain.Entities;

/// <summary>
/// Pojedyncza sesja lekcji użytkownika.
/// </summary>
public sealed class Lesson
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public User User { get; init; } = null!;

    /// <summary>Rodzaj lekcji (np. flashcards, typing).</summary>
    public string LessonKind { get; init; } = string.Empty;

    /// <summary>Zaplanowana liczba słów w sesji.</summary>
    public int WordCount { get; init; }

    public DateTime StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public List<LessonRepetition> Repetitions { get; init; } = [];
}
