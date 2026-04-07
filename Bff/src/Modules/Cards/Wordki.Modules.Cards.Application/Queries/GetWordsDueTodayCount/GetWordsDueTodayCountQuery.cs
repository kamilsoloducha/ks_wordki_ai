using MediatR;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Queries;

namespace Wordki.Modules.Cards.Application.Queries.GetWordsDueTodayCount;

/// <param name="QuestionSideType">When set with <see cref="AnswerSideType"/>, only cards in groups whose front/back types match this lesson direction (question/answer either way).</param>
public sealed record GetWordsDueTodayCountQuery(
    Guid UserId,
    string? QuestionSideType = null,
    string? AnswerSideType = null,
    LessonWordSource WordSource = LessonWordSource.Review) : IRequest<Result<int>>;
