using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Commands.UpdateCard;

public sealed record UpdateCardCommand(
    Guid UserId,
    long CardId,
    string FrontLabel,
    string BackLabel,
    string? FrontExample,
    string? FrontComment,
    string? BackExample,
    string? BackComment) : IRequest<Result<UpdateCardResult>>;

public sealed record UpdateCardResult(
    long Id,
    long GroupId,
    CardSidePayload Front,
    CardSidePayload Back);

public sealed record CardSidePayload(string Label, string Example, string Comment);

public sealed class UpdateCardCommandHandler(ICardsDbContext dbContext)
    : IRequestHandler<UpdateCardCommand, Result<UpdateCardResult>>
{
    public async Task<Result<UpdateCardResult>> Handle(
        UpdateCardCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = UpdateCardCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<UpdateCardResult>.Failure(validationResult.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<UpdateCardResult>.Failure(new AppError(
                "cards.update_card.user.not_found",
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
            return Result<UpdateCardResult>.Failure(new AppError(
                "cards.update_card.not_found",
                "Card was not found.",
                ErrorType.NotFound,
                "cardId"));
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == card.GroupId, cancellationToken);

        if (group is null || group.UserId != cardsUserId)
        {
            return Result<UpdateCardResult>.Failure(new AppError(
                "cards.update_card.forbidden",
                "You cannot update this card.",
                ErrorType.Forbidden,
                "cardId"));
        }

        static string Opt(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        card.FrontSide.Label = request.FrontLabel.Trim();
        card.FrontSide.Example = Opt(request.FrontExample);
        card.FrontSide.Comment = Opt(request.FrontComment);

        card.BackSide.Label = request.BackLabel.Trim();
        card.BackSide.Example = Opt(request.BackExample);
        card.BackSide.Comment = Opt(request.BackComment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateCardResult>.Success(
            new UpdateCardResult(
                card.Id,
                card.GroupId,
                new CardSidePayload(card.FrontSide.Label, card.FrontSide.Example, card.FrontSide.Comment),
                new CardSidePayload(card.BackSide.Label, card.BackSide.Example, card.BackSide.Comment)));
    }
}
