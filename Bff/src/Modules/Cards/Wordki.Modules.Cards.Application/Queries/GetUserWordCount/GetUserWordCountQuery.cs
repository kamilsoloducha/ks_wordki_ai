using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Queries.GetUserWordCount;

public sealed record GetUserWordCountQuery(Guid UserId) : IRequest<Result<int>>;

public sealed class GetUserWordCountQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetUserWordCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetUserWordCountQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetUserWordCountQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<int>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<int>.Failure(new AppError(
                "cards.get_word_count.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var count = await (
            from card in dbContext.Cards.AsNoTracking()
            join g in dbContext.Groups.AsNoTracking() on card.GroupId equals g.Id
            where g.UserId == cardsUserId
            select card).CountAsync(cancellationToken);

        return Result<int>.Success(count);
    }
}
