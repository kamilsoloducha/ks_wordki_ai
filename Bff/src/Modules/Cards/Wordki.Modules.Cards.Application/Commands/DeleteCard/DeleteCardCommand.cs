using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Commands.DeleteCard;

public sealed record DeleteCardCommand(Guid UserId, long CardId) : IRequest<Result>;

public sealed class DeleteCardCommandHandler(ICardsDbContext dbContext)
    : IRequestHandler<DeleteCardCommand, Result>
{
    public async Task<Result> Handle(DeleteCardCommand request, CancellationToken cancellationToken)
    {
        var validationResult = DeleteCardCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result.Failure(new AppError(
                "cards.delete_card.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var card = await dbContext.Cards
            .Include(c => c.FrontSide)
            .Include(c => c.BackSide)
            .SingleOrDefaultAsync(x => x.Id == request.CardId, cancellationToken);

        if (card is null)
        {
            return Result.Failure(new AppError(
                "cards.delete_card.not_found",
                "Card was not found.",
                ErrorType.NotFound,
                "cardId"));
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == card.GroupId, cancellationToken);

        if (group is null || group.UserId != cardsUserId)
        {
            return Result.Failure(new AppError(
                "cards.delete_card.forbidden",
                "You cannot delete this card.",
                ErrorType.Forbidden,
                "cardId"));
        }

        dbContext.Cards.Remove(card);
        dbContext.CardSides.Remove(card.FrontSide);
        dbContext.CardSides.Remove(card.BackSide);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
