using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Queries.SearchCards;

public static class SearchCardsQueryValidator
{
    private const int DrawerMax = 10_000;
    private const int PageSizeMax = 500;

    public static Result Validate(SearchCardsQuery query)
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

        if (!query.GetCount && !query.GetList)
        {
            errors.Add(new AppError(
                "cards.validation.search.response.none",
                "At least one of getCount or getList must be true.",
                ErrorType.Validation,
                null));
        }

        if (query.Drawer is int d && (d < 0 || d > DrawerMax))
        {
            errors.Add(new AppError(
                "cards.validation.drawer.invalid",
                $"Drawer must be between 0 and {DrawerMax}.",
                ErrorType.Validation,
                "drawer"));
        }

        if (query.GroupId is long gid && gid <= 0)
        {
            errors.Add(new AppError(
                "cards.validation.group_id.invalid",
                "Group id must be a positive number when provided.",
                ErrorType.Validation,
                "groupId"));
        }

        if (query.Page < 1)
        {
            errors.Add(new AppError(
                "cards.validation.page.invalid",
                "Page must be at least 1.",
                ErrorType.Validation,
                "page"));
        }

        if (query.PageSize < 1 || query.PageSize > PageSizeMax)
        {
            errors.Add(new AppError(
                "cards.validation.page_size.invalid",
                $"Page size must be between 1 and {PageSizeMax}.",
                ErrorType.Validation,
                "pageSize"));
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(errors);
    }
}
