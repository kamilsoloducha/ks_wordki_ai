using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.GetDueTodayCards;

public static class GetDueTodayCardsQueryValidator
{
    public const int MaxLimit = 500;

    public static Result Validate(GetDueTodayCardsQuery query)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        var hasQ = query.QuestionSideType is not null;
        var hasA = query.AnswerSideType is not null;
        if (hasQ != hasA)
        {
            return Result.Failure(new AppError(
                "cards.validation.lesson_direction.partial",
                "Both questionSideType and answerSideType must be provided together, or omit both.",
                ErrorType.Validation,
                hasQ ? "answerSideType" : "questionSideType"));
        }

        if (query.Limit < 1 || query.Limit > MaxLimit)
        {
            return Result.Failure(new AppError(
                "cards.validation.due_today_cards.limit",
                $"Limit must be between 1 and {MaxLimit}.",
                ErrorType.Validation,
                "limit"));
        }

        return Result.Success();
    }
}
