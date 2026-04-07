using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.GetDistinctSideTypePairs;

public static class GetDistinctSideTypePairsQueryValidator
{
    public static Result Validate(GetDistinctSideTypePairsQuery query)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        return Result.Success();
    }
}
