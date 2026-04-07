using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Application.Queries.SearchCards;

public sealed record SearchCardsQuery(
    Guid UserId,
    int? Drawer,
    long? GroupId,
    bool GetCount,
    bool GetList,
    int Page,
    int PageSize)
    : IRequest<Result<SearchCardsQueryResult>>;

public sealed record SearchCardsQueryResult(int? Count, IReadOnlyList<SearchCardsItem>? Items);

public sealed record SearchCardsItem(
    long Id,
    long GroupId,
    string FrontLabel,
    string FrontExample,
    string FrontComment,
    string BackLabel,
    string BackExample,
    string BackComment);

public sealed class SearchCardsQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<SearchCardsQuery, Result<SearchCardsQueryResult>>
{
    public async Task<Result<SearchCardsQueryResult>> Handle(
        SearchCardsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = SearchCardsQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<SearchCardsQueryResult>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<SearchCardsQueryResult>.Failure(new AppError(
                "cards.search_cards.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        if (request.GroupId is long groupId)
        {
            var group = await dbContext.Groups
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);

            if (group is null)
            {
                return Result<SearchCardsQueryResult>.Failure(new AppError(
                    "cards.search_cards.group.not_found",
                    "Group was not found.",
                    ErrorType.NotFound,
                    "groupId"));
            }

            if (group.UserId != cardsUserId)
            {
                return Result<SearchCardsQueryResult>.Failure(new AppError(
                    "cards.search_cards.group.forbidden",
                    "You cannot search cards in this group.",
                    ErrorType.Forbidden,
                    "groupId"));
            }
        }

        var baseQuery = BuildFilteredCardsQuery(dbContext, cardsUserId, request);

        if (request.GetCount && request.GetList)
        {
            var total = await baseQuery.CountAsync(cancellationToken);
            var items = await ProjectToItemsPaged(baseQuery, request.Page, request.PageSize)
                .ToListAsync(cancellationToken);
            return Result<SearchCardsQueryResult>.Success(new SearchCardsQueryResult(total, items));
        }

        if (request.GetCount)
        {
            var count = await baseQuery.CountAsync(cancellationToken);
            return Result<SearchCardsQueryResult>.Success(new SearchCardsQueryResult(count, null));
        }

        var listOnly = await ProjectToItemsPaged(baseQuery, request.Page, request.PageSize)
            .ToListAsync(cancellationToken);
        return Result<SearchCardsQueryResult>.Success(new SearchCardsQueryResult(null, listOnly));
    }

    private static IQueryable<Card> BuildFilteredCardsQuery(
        ICardsDbContext dbContext,
        long cardsUserId,
        SearchCardsQuery request)
    {
        var q = dbContext.Cards
            .AsNoTracking()
            .Where(c =>
                dbContext.Groups.Any(g => g.Id == c.GroupId && g.UserId == cardsUserId));

        if (request.GroupId is long gid)
        {
            q = q.Where(c => c.GroupId == gid);
        }

        if (request.Drawer is int drawer)
        {
            q = q.Where(c =>
                dbContext.Results.Any(r =>
                    r.UserId == cardsUserId
                    && r.Drawer == drawer
                    && r.NextRepeatUtc != null
                    && (r.CardSideId == c.FrontSideId || r.CardSideId == c.BackSideId)));
        }

        return q;
    }

    private static IQueryable<SearchCardsItem> ProjectToItems(IQueryable<Card> baseQuery) =>
        baseQuery
            .OrderBy(c => c.GroupId)
            .ThenBy(c => c.Id)
            .Select(c => new SearchCardsItem(
                c.Id,
                c.GroupId,
                c.FrontSide.Label,
                c.FrontSide.Example,
                c.FrontSide.Comment,
                c.BackSide.Label,
                c.BackSide.Example,
                c.BackSide.Comment));

    private static IQueryable<SearchCardsItem> ProjectToItemsPaged(
        IQueryable<Card> baseQuery,
        int page,
        int pageSize)
    {
        var skip = (page - 1) * pageSize;
        return ProjectToItems(baseQuery).Skip(skip).Take(pageSize);
    }
}
