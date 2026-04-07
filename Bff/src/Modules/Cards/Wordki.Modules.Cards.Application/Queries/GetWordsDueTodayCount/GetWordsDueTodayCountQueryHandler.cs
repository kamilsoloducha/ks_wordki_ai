using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Application.Queries;

namespace Wordki.Modules.Cards.Application.Queries.GetWordsDueTodayCount;

public sealed class GetWordsDueTodayCountQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetWordsDueTodayCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetWordsDueTodayCountQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetWordsDueTodayCountQueryValidator.Validate(request);
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
                "cards.get_due_today_count.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var utcNow = DateTime.UtcNow;
        var endOfTodayUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(1);

        var hasDirection =
            request.QuestionSideType is not null && request.AnswerSideType is not null;

        var isNewWords = request.WordSource == LessonWordSource.NewWords;

        if (hasDirection)
        {
            var q = request.QuestionSideType!.Trim();
            var a = request.AnswerSideType!.Trim();

            var joined = dbContext.Cards
                .AsNoTracking()
                .Join(dbContext.Groups.AsNoTracking(), c => c.GroupId, g => g.Id, (c, g) => new { c, g })
                .Where(x => x.g.UserId == cardsUserId
                    && ((x.g.FrontSideType == q && x.g.BackSideType == a)
                        || (x.g.BackSideType == q && x.g.FrontSideType == a)));

            joined = isNewWords
                ? joined.Where(x => dbContext.Results.Any(r =>
                    r.UserId == cardsUserId
                    && r.CardSideId ==
                    (x.g.FrontSideType == q && x.g.BackSideType == a
                        ? x.c.FrontSideId
                        : x.c.BackSideId)
                    && r.NextRepeatUtc == null))
                : joined.Where(x => dbContext.Results.Any(r =>
                    r.UserId == cardsUserId
                    && (r.CardSideId == x.c.FrontSideId || r.CardSideId == x.c.BackSideId)
                    && r.NextRepeatUtc != null
                    && r.NextRepeatUtc < endOfTodayUtc));

            var count = await joined.CountAsync(cancellationToken);
            return Result<int>.Success(count);
        }

        var baseNoDir = dbContext.Cards
            .AsNoTracking()
            .Where(c =>
                dbContext.Groups.Any(g => g.Id == c.GroupId && g.UserId == cardsUserId));

        baseNoDir = isNewWords
            ? baseNoDir.Where(c => dbContext.Results.Any(r =>
                r.UserId == cardsUserId
                && (r.CardSideId == c.FrontSideId || r.CardSideId == c.BackSideId)
                && r.NextRepeatUtc == null))
            : baseNoDir.Where(c => dbContext.Results.Any(r =>
                r.UserId == cardsUserId
                && (r.CardSideId == c.FrontSideId || r.CardSideId == c.BackSideId)
                && r.NextRepeatUtc != null
                && r.NextRepeatUtc < endOfTodayUtc));

        var countNoDir = await baseNoDir.CountAsync(cancellationToken);
        return Result<int>.Success(countNoDir);
    }
}
