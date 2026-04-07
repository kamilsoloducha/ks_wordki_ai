using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Queries.GetGroupCards;

public sealed record GetGroupCardsQuery(Guid UserId, long GroupId)
    : IRequest<Result<IReadOnlyList<GetGroupCardsItem>>>;

public sealed record GetGroupCardsItem(
    long Id,
    long GroupId,
    string FrontLabel,
    string FrontExample,
    string FrontComment,
    string BackLabel,
    string BackExample,
    string BackComment);

public sealed class GetGroupCardsQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetGroupCardsQuery, Result<IReadOnlyList<GetGroupCardsItem>>>
{
    public async Task<Result<IReadOnlyList<GetGroupCardsItem>>> Handle(
        GetGroupCardsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetGroupCardsQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<GetGroupCardsItem>>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<IReadOnlyList<GetGroupCardsItem>>.Failure(new AppError(
                "cards.get_group_cards.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
        {
            return Result<IReadOnlyList<GetGroupCardsItem>>.Failure(new AppError(
                "cards.get_group_cards.group.not_found",
                "Group was not found.",
                ErrorType.NotFound,
                "groupId"));
        }

        if (group.UserId != cardsUserId)
        {
            return Result<IReadOnlyList<GetGroupCardsItem>>.Failure(new AppError(
                "cards.get_group_cards.forbidden",
                "You cannot view cards in this group.",
                ErrorType.Forbidden,
                "groupId"));
        }

        var cards = await dbContext.Cards
            .AsNoTracking()
            .Where(c => c.GroupId == request.GroupId)
            .OrderBy(c => c.Id)
            .Select(c => new GetGroupCardsItem(
                c.Id,
                c.GroupId,
                c.FrontSide.Label,
                c.FrontSide.Example,
                c.FrontSide.Comment,
                c.BackSide.Label,
                c.BackSide.Example,
                c.BackSide.Comment))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<GetGroupCardsItem>>.Success(cards);
    }
}
