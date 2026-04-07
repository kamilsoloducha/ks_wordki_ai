using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Queries.GetUserCardGroups;

public sealed record GetUserCardGroupsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<GetUserCardGroupsItem>>>;

public sealed record GetUserCardGroupsItem(
    long Id,
    string Name,
    string FrontSideType,
    string BackSideType,
    int CardCount);

public sealed class GetUserCardGroupsQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetUserCardGroupsQuery, Result<IReadOnlyList<GetUserCardGroupsItem>>>
{
    public async Task<Result<IReadOnlyList<GetUserCardGroupsItem>>> Handle(
        GetUserCardGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetUserCardGroupsQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<GetUserCardGroupsItem>>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<IReadOnlyList<GetUserCardGroupsItem>>.Failure(new AppError(
                "cards.get_groups.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var items = await dbContext.Groups
            .AsNoTracking()
            .Where(g => g.UserId == cardsUserId)
            .OrderBy(g => g.Id)
            .Select(g => new GetUserCardGroupsItem(
                g.Id,
                g.Name,
                g.FrontSideType,
                g.BackSideType,
                g.Cards.Count))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<GetUserCardGroupsItem>>.Success(items);
    }
}
