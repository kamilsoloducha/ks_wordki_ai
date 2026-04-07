using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Lessons.Application.Commands.AddLessonRepetition;

public static class AddLessonRepetitionCommandValidator
{
    public static Result Validate(AddLessonRepetitionCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "lessons.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (command.LessonId < 1)
        {
            return Result.Failure(new AppError(
                "lessons.validation.lesson_id.invalid",
                "Lesson id must be positive.",
                ErrorType.Validation,
                "lessonId"));
        }

        if (command.QuestionResultId < 1)
        {
            return Result.Failure(new AppError(
                "lessons.validation.question_result_id.invalid",
                "Question result id must be positive.",
                ErrorType.Validation,
                "questionResultId"));
        }

        return Result.Success();
    }
}
