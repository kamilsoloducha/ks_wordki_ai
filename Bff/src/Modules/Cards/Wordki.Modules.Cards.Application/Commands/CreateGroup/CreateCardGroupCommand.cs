using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Application.Commands.CreateGroup;

public sealed record CreateCardGroupCommand(Guid UserId, string Name, string FrontSideType, string BackSideType)
    : IRequest<Result<CreateCardGroupResult>>;

public sealed record CreateCardGroupResult(long Id, string Name, string FrontSideType, string BackSideType);

public sealed class CreateCardGroupCommandHandler(ICardsDbContext dbContext)
    : IRequestHandler<CreateCardGroupCommand, Result<CreateCardGroupResult>>
{
    public async Task<Result<CreateCardGroupResult>> Handle(
        CreateCardGroupCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = CreateCardGroupCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<CreateCardGroupResult>.Failure(validationResult.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<CreateCardGroupResult>.Failure(new AppError(
                "cards.create_group.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var entity = new Group
        {
            Name = request.Name.Trim(),
            FrontSideType = request.FrontSideType.Trim(),
            BackSideType = request.BackSideType.Trim(),
            Type = GroupType.UserOwned,
            UserId = cardsUserId,
            Cards = []
        };

        await dbContext.Groups.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateCardGroupResult>.Success(
            new CreateCardGroupResult(entity.Id, entity.Name, entity.FrontSideType, entity.BackSideType));
    }
}
