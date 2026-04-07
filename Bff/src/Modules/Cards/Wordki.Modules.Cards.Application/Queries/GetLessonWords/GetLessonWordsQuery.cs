using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Queries.GetLessonWords;

public sealed record GetLessonWordsQuery(
    Guid UserId,
    long GroupId,
    string QuestionSideType,
    string AnswerSideType,
    int Limit) : IRequest<Result<IReadOnlyList<GetLessonWordsItem>>>;

public sealed record GetLessonWordsItem(
    long QuestionResultId,
    int QuestionDrawer,
    string QuestionLabel,
    string QuestionExample,
    string AnswerLabel,
    string AnswerExample);

public sealed class GetLessonWordsQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetLessonWordsQuery, Result<IReadOnlyList<GetLessonWordsItem>>>
{
    public async Task<Result<IReadOnlyList<GetLessonWordsItem>>> Handle(
        GetLessonWordsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetLessonWordsQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<GetLessonWordsItem>>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<IReadOnlyList<GetLessonWordsItem>>.Failure(new AppError(
                "cards.get_lesson_words.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
        {
            return Result<IReadOnlyList<GetLessonWordsItem>>.Failure(new AppError(
                "cards.get_lesson_words.group.not_found",
                "Group was not found.",
                ErrorType.NotFound,
                "groupId"));
        }

        if (group.UserId != cardsUserId)
        {
            return Result<IReadOnlyList<GetLessonWordsItem>>.Failure(new AppError(
                "cards.get_lesson_words.forbidden",
                "You cannot use this group for a lesson.",
                ErrorType.Forbidden,
                "groupId"));
        }

        var q = request.QuestionSideType.Trim();
        var a = request.AnswerSideType.Trim();
        var matchesForward =
            group.FrontSideType == q && group.BackSideType == a;
        var matchesBackward =
            group.BackSideType == q && group.FrontSideType == a;

        if (!matchesForward && !matchesBackward)
        {
            return Result<IReadOnlyList<GetLessonWordsItem>>.Failure(new AppError(
                "cards.get_lesson_words.direction.mismatch",
                "Question and answer side types must match this group's front and back types (in either order).",
                ErrorType.Validation,
                "questionSideType"));
        }

        var questionSideIsFront = matchesForward;

        var utcNow = DateTime.UtcNow;
        var endOfTodayUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(1);

        var items = await dbContext.Cards
            .AsNoTracking()
            .Where(c =>
                c.GroupId == request.GroupId
                && dbContext.Results.Any(r =>
                    r.UserId == cardsUserId
                    && (r.CardSideId == c.FrontSideId || r.CardSideId == c.BackSideId)
                    && r.NextRepeatUtc != null
                    && r.NextRepeatUtc < endOfTodayUtc)
                && dbContext.Results.Any(r =>
                    r.UserId == cardsUserId
                    && r.CardSideId == (questionSideIsFront ? c.FrontSideId : c.BackSideId)
                    && r.NextRepeatUtc != null))
            .OrderBy(c => c.Id)
            .Take(request.Limit)
            .Select(c => new GetLessonWordsItem(
                dbContext.Results
                    .Where(r =>
                        r.UserId == cardsUserId
                        && r.NextRepeatUtc != null
                        && r.CardSideId == (questionSideIsFront ? c.FrontSideId : c.BackSideId))
                    .Select(r => r.Id)
                    .First(),
                dbContext.Results
                    .Where(r =>
                        r.UserId == cardsUserId
                        && r.NextRepeatUtc != null
                        && r.CardSideId == (questionSideIsFront ? c.FrontSideId : c.BackSideId))
                    .Select(r => r.Drawer)
                    .First(),
                questionSideIsFront ? c.FrontSide.Label : c.BackSide.Label,
                questionSideIsFront ? c.FrontSide.Example : c.BackSide.Example,
                questionSideIsFront ? c.BackSide.Label : c.FrontSide.Label,
                questionSideIsFront ? c.BackSide.Example : c.FrontSide.Example))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<GetLessonWordsItem>>.Success(items);
    }
}
