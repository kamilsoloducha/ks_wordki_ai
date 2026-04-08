using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Commands.TickCardResult;

public sealed record TickCardResultCommand(Guid UserId, long ResultId) : IRequest<Result>;

public sealed class TickCardResultCommandHandler(ICardsDbContext dbContext)
    : IRequestHandler<TickCardResultCommand, Result>
{
    public async Task<Result> Handle(TickCardResultCommand request, CancellationToken cancellationToken)
    {
        var validation = TickCardResultCommandValidator.Validate(request);
        if (validation.IsFailure)
        {
            return validation;
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result.Failure(new AppError(
                "cards.tick_result.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var resultEntity = await dbContext.Results
            .SingleOrDefaultAsync(r => r.Id == request.ResultId, cancellationToken);

        if (resultEntity is null)
        {
            return Result.Failure(new AppError(
                "cards.tick_result.not_found",
                "Result was not found.",
                ErrorType.NotFound,
                "resultId"));
        }

        if (resultEntity.UserId != cardsUserId)
        {
            return Result.Failure(new AppError(
                "cards.tick_result.forbidden",
                "You cannot modify this result.",
                ErrorType.Forbidden,
                "resultId"));
        }

        resultEntity.SetTicked(true);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
