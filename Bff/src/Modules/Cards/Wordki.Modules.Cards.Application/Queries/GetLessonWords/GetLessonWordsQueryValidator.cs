using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.GetLessonWords;

public static class GetLessonWordsQueryValidator
{
    public const int MaxLimit = 500;

    public static Result Validate(GetLessonWordsQuery query)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (query.GroupId < 1)
        {
            return Result.Failure(new AppError(
                "cards.validation.group_id.invalid",
                "Group id must be positive.",
                ErrorType.Validation,
                "groupId"));
        }

        if (string.IsNullOrWhiteSpace(query.QuestionSideType))
        {
            return Result.Failure(new AppError(
                "cards.validation.lesson_words.question_side_type.required",
                "Question side type is required.",
                ErrorType.Validation,
                "questionSideType"));
        }

        if (string.IsNullOrWhiteSpace(query.AnswerSideType))
        {
            return Result.Failure(new AppError(
                "cards.validation.lesson_words.answer_side_type.required",
                "Answer side type is required.",
                ErrorType.Validation,
                "answerSideType"));
        }

        if (query.Limit < 1 || query.Limit > MaxLimit)
        {
            return Result.Failure(new AppError(
                "cards.validation.lesson_words.limit",
                $"Limit must be between 1 and {MaxLimit}.",
                ErrorType.Validation,
                "limit"));
        }

        return Result.Success();
    }
}
