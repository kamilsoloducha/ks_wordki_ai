namespace Wordki.Modules.Lessons.Api.Requests;

public sealed record CreateLessonRequest(string LessonKind, int WordCount);

/// <param name="Result">Czy użytkownik znał odpowiedź (true = znał).</param>
public sealed record AddLessonRepetitionRequest(long QuestionResultId, bool Result);

public sealed record SubmitLessonAnswerRequest(Guid CardId, bool IsCorrect, string? UserAnswer);
