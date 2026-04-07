using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Application.Queries;

namespace Wordki.Modules.Cards.Application.Queries.GetDueTodayCards;

public sealed class GetDueTodayCardsQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetDueTodayCardsQuery, Result<IReadOnlyList<GetDueTodayCardsItem>>>
{
    public async Task<Result<IReadOnlyList<GetDueTodayCardsItem>>> Handle(
        GetDueTodayCardsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetDueTodayCardsQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<GetDueTodayCardsItem>>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<IReadOnlyList<GetDueTodayCardsItem>>.Failure(new AppError(
                "cards.get_due_today_cards.user.not_found",
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

            var items = await joined
                .OrderBy(x => x.c.GroupId)
                .ThenBy(x => x.c.Id)
                .Take(request.Limit)
                .Select(x => new GetDueTodayCardsItem(
                    x.c.Id,
                    x.c.GroupId,
                    x.c.FrontSide.Label,
                    x.c.FrontSide.Example,
                    x.c.FrontSide.Comment,
                    x.c.BackSide.Label,
                    x.c.BackSide.Example,
                    x.c.BackSide.Comment,
                    dbContext.Results
                        .Where(r =>
                            r.UserId == cardsUserId
                            && r.CardSideId ==
                            (x.g.FrontSideType == q && x.g.BackSideType == a
                                ? x.c.FrontSideId
                                : x.c.BackSideId)
                            && (isNewWords ? r.NextRepeatUtc == null : r.NextRepeatUtc != null))
                        .Select(r => (long?)r.Id)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<GetDueTodayCardsItem>>.Success(items);
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

        var itemsNoDirection = await baseNoDir
            .OrderBy(c => c.GroupId)
            .ThenBy(c => c.Id)
            .Take(request.Limit)
            .Select(c => new GetDueTodayCardsItem(
                c.Id,
                c.GroupId,
                c.FrontSide.Label,
                c.FrontSide.Example,
                c.FrontSide.Comment,
                c.BackSide.Label,
                c.BackSide.Example,
                c.BackSide.Comment,
                (long?)null))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<GetDueTodayCardsItem>>.Success(itemsNoDirection);
    }
}
