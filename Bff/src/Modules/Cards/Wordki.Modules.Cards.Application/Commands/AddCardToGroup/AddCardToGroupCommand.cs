using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Domain.Entities;
using CardResult = Wordki.Modules.Cards.Domain.Entities.Result;

namespace Wordki.Modules.Cards.Application.Commands.AddCardToGroup;

public sealed record AddCardToGroupCommand(
    Guid UserId,
    long GroupId,
    string FrontLabel,
    string BackLabel,
    string? FrontExample,
    string? FrontComment,
    string? BackExample,
    string? BackComment) : IRequest<Result<AddCardToGroupResult>>;

public sealed record AddCardToGroupResult(long Id, long GroupId, CardSidePayload Front, CardSidePayload Back);

public sealed record CardSidePayload(string Label, string Example, string Comment);

public sealed class AddCardToGroupCommandHandler(ICardsDbContext dbContext)
    : IRequestHandler<AddCardToGroupCommand, Result<AddCardToGroupResult>>
{
    public async Task<Result<AddCardToGroupResult>> Handle(
        AddCardToGroupCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = AddCardToGroupCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<AddCardToGroupResult>.Failure(validationResult.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<AddCardToGroupResult>.Failure(new AppError(
                "cards.add_card.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var group = await dbContext.Groups
            .SingleOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
        {
            return Result<AddCardToGroupResult>.Failure(new AppError(
                "cards.add_card.group.not_found",
                "Group was not found.",
                ErrorType.NotFound,
                "groupId"));
        }

        if (group.UserId != cardsUserId)
        {
            return Result<AddCardToGroupResult>.Failure(new AppError(
                "cards.add_card.group.forbidden",
                "You cannot add cards to this group.",
                ErrorType.Forbidden,
                "groupId"));
        }

        static string Opt(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        var frontSide = new CardSide
        {
            Label = request.FrontLabel.Trim(),
            Example = Opt(request.FrontExample),
            Comment = Opt(request.FrontComment)
        };

        var backSide = new CardSide
        {
            Label = request.BackLabel.Trim(),
            Example = Opt(request.BackExample),
            Comment = Opt(request.BackComment)
        };

        await dbContext.CardSides.AddAsync(frontSide, cancellationToken);
        await dbContext.CardSides.AddAsync(backSide, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var frontResult = CardResult.CreateInitial(cardsUserId, group.Id, frontSide.Id);
        var backResult = CardResult.CreateInitial(cardsUserId, group.Id, backSide.Id);
        await dbContext.Results.AddAsync(frontResult, cancellationToken);
        await dbContext.Results.AddAsync(backResult, cancellationToken);

        var card = new Card
        {
            GroupId = group.Id,
            FrontSideId = frontSide.Id,
            BackSideId = backSide.Id,
            FrontSide = frontSide,
            BackSide = backSide
        };

        await dbContext.Cards.AddAsync(card, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AddCardToGroupResult>.Success(
            new AddCardToGroupResult(
                card.Id,
                card.GroupId,
                new CardSidePayload(frontSide.Label, frontSide.Example, frontSide.Comment),
                new CardSidePayload(backSide.Label, backSide.Example, backSide.Comment)));
    }
}
