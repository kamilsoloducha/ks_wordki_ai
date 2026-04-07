using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Commands.UpdateCardGroup;

public sealed record UpdateCardGroupCommand(
    Guid UserId,
    long GroupId,
    string Name,
    string FrontSideType,
    string BackSideType) : IRequest<Result<UpdateCardGroupResult>>;

public sealed record UpdateCardGroupResult(long Id, string Name, string FrontSideType, string BackSideType);

public sealed class UpdateCardGroupCommandHandler(ICardsDbContext dbContext)
    : IRequestHandler<UpdateCardGroupCommand, Result<UpdateCardGroupResult>>
{
    public async Task<Result<UpdateCardGroupResult>> Handle(
        UpdateCardGroupCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = UpdateCardGroupCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<UpdateCardGroupResult>.Failure(validationResult.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<UpdateCardGroupResult>.Failure(new AppError(
                "cards.update_group.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var group = await dbContext.Groups
            .SingleOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
        {
            return Result<UpdateCardGroupResult>.Failure(new AppError(
                "cards.update_group.not_found",
                "Group was not found.",
                ErrorType.NotFound,
                "groupId"));
        }

        if (group.UserId != cardsUserId)
        {
            return Result<UpdateCardGroupResult>.Failure(new AppError(
                "cards.update_group.forbidden",
                "You cannot update this group.",
                ErrorType.Forbidden,
                "groupId"));
        }

        group.Name = request.Name.Trim();
        group.FrontSideType = request.FrontSideType.Trim();
        group.BackSideType = request.BackSideType.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateCardGroupResult>.Success(
            new UpdateCardGroupResult(
                group.Id,
                group.Name,
                group.FrontSideType,
                group.BackSideType));
    }
}
