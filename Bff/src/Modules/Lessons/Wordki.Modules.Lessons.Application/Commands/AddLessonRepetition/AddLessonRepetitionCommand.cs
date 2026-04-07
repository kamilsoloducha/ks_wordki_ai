using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Lessons.Application.Abstractions;
using Wordki.Modules.Lessons.Domain.Entities;

namespace Wordki.Modules.Lessons.Application.Commands.AddLessonRepetition;

public sealed record AddLessonRepetitionCommand(
    Guid UserId,
    long LessonId,
    long QuestionResultId,
    bool Result)
    : IRequest<Result<AddLessonRepetitionResult>>;

public sealed record AddLessonRepetitionResult(long Id, DateTime AnsweredAtUtc);

public sealed class AddLessonRepetitionCommandHandler(
    ILessonsDbContext dbContext,
    TimeProvider timeProvider,
    ISharedEventOutboxWriter sharedEventOutboxWriter)
    : IRequestHandler<AddLessonRepetitionCommand, Result<AddLessonRepetitionResult>>
{
    public async Task<Result<AddLessonRepetitionResult>> Handle(
        AddLessonRepetitionCommand request,
        CancellationToken cancellationToken)
    {
        var validation = AddLessonRepetitionCommandValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<AddLessonRepetitionResult>.Failure(validation.Errors);
        }

        var lessonsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (lessonsUserId == 0)
        {
            return Result<AddLessonRepetitionResult>.Failure(new AppError(
                "lessons.add_repetition.user.not_found",
                "User was not found in the lessons module.",
                ErrorType.NotFound,
                "userId"));
        }

        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.LessonId, cancellationToken);

        if (lesson is null)
        {
            return Result<AddLessonRepetitionResult>.Failure(new AppError(
                "lessons.add_repetition.lesson.not_found",
                "Lesson was not found.",
                ErrorType.NotFound,
                "lessonId"));
        }

        if (lesson.UserId != lessonsUserId)
        {
            return Result<AddLessonRepetitionResult>.Failure(new AppError(
                "lessons.add_repetition.lesson.forbidden",
                "You cannot add repetitions to this lesson.",
                ErrorType.Forbidden,
                "lessonId"));
        }

        var maxSeq = await dbContext.LessonRepetitions
            .AsNoTracking()
            .Where(x => x.LessonId == request.LessonId)
            .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken);
        var nextSequence = (maxSeq ?? 0) + 1;

        var answeredAt = timeProvider.GetUtcNow().UtcDateTime;
        var repetition = new LessonRepetition
        {
            LessonId = request.LessonId,
            SequenceNumber = nextSequence,
            QuestionResultId = request.QuestionResultId,
            IsKnown = request.Result,
            AnsweredAtUtc = answeredAt
        };

        await dbContext.LessonRepetitions.AddAsync(repetition, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await sharedEventOutboxWriter.AddAndSaveAsync(
            [
                new SharedEventMessage
                {
                    PublisherName = "Lessons",
                    ConsumerName = OutboxRouting.Broadcast,
                    DataType = "RepeatAdded",
                    AddedAtUtc = answeredAt,
                    Payload = JsonSerializer.Serialize(
                        new RepeatAddedIntegrationEvent(
                            request.QuestionResultId,
                            request.Result,
                            request.UserId))
                }
            ],
            cancellationToken);

        return Result<AddLessonRepetitionResult>.Success(
            new AddLessonRepetitionResult(repetition.Id, repetition.AnsweredAtUtc));
    }
}
