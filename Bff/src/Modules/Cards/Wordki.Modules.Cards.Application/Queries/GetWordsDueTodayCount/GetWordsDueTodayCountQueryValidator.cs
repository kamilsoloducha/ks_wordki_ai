using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.GetWordsDueTodayCount;

public static class GetWordsDueTodayCountQueryValidator
{
    public static Result Validate(GetWordsDueTodayCountQuery query)
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

        return Result.Success();
    }
}
