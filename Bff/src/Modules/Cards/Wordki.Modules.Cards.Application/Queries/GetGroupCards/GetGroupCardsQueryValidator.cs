using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.GetGroupCards;

public static class GetGroupCardsQueryValidator
{
    public static Result Validate(GetGroupCardsQuery query)
    {
        var errors = new List<AppError>();

        if (query.UserId == Guid.Empty)
        {
            errors.Add(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (query.GroupId <= 0)
        {
            errors.Add(new AppError(
                "cards.validation.group_id.invalid",
                "Group id must be a positive number.",
                ErrorType.Validation,
                "groupId"));
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(errors);
    }
}
