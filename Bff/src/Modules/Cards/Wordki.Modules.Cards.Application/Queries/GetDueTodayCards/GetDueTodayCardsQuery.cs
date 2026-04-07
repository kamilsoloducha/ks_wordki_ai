using MediatR;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Queries;

namespace Wordki.Modules.Cards.Application.Queries.GetDueTodayCards;

public sealed record GetDueTodayCardsQuery(
    Guid UserId,
    string? QuestionSideType = null,
    string? AnswerSideType = null,
    int Limit = 20,
    LessonWordSource WordSource = LessonWordSource.Review) : IRequest<Result<IReadOnlyList<GetDueTodayCardsItem>>>;

public sealed record GetDueTodayCardsItem(
    long Id,
    long GroupId,
    string FrontLabel,
    string FrontExample,
    string FrontComment,
    string BackLabel,
    string BackExample,
    string BackComment,
    long? QuestionResultId);
