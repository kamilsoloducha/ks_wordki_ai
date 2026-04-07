using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Lessons.Application.Commands.CreateLesson;

public static class CreateLessonCommandValidator
{
    private const int LessonKindMaxLength = 100;
    public const int MaxWordCount = 10_000;

    public static Result Validate(CreateLessonCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "lessons.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (string.IsNullOrWhiteSpace(command.LessonKind))
        {
            return Result.Failure(new AppError(
                "lessons.validation.lesson_kind.required",
                "Lesson kind is required.",
                ErrorType.Validation,
                "lessonKind"));
        }

        if (command.LessonKind.Trim().Length > LessonKindMaxLength)
        {
            return Result.Failure(new AppError(
                "lessons.validation.lesson_kind.too_long",
                $"Lesson kind cannot exceed {LessonKindMaxLength} characters.",
                ErrorType.Validation,
                "lessonKind"));
        }

        if (command.WordCount < 1 || command.WordCount > MaxWordCount)
        {
            return Result.Failure(new AppError(
                "lessons.validation.word_count.range",
                $"Word count must be between 1 and {MaxWordCount}.",
                ErrorType.Validation,
                "wordCount"));
        }

        return Result.Success();
    }
}
