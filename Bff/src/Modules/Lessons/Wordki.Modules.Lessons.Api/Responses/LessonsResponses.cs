namespace Wordki.Modules.Lessons.Api.Responses;

public sealed record CreateLessonResponse(long Id);

public sealed record AddLessonRepetitionResponse(long Id, DateTime AnsweredAtUtc);

public sealed record NextLessonCardDto(
    Guid SessionId,
    Guid CardId,
    string PromptLabel,
    string PromptExample,
    string PromptComment);

public sealed record SubmitLessonAnswerResponse(Guid SessionId, Guid CardId, bool Accepted, DateTime ProcessedAtUtc);
