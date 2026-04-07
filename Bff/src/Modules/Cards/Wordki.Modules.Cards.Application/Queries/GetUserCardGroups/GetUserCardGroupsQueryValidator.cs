using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.GetUserCardGroups;

public static class GetUserCardGroupsQueryValidator
{
    public static Result Validate(GetUserCardGroupsQuery query)
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
