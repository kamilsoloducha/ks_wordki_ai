using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Lessons.Application.Abstractions;
using Wordki.Modules.Lessons.Domain.Entities;

namespace Wordki.Modules.Lessons.Application.Commands.CreateLesson;

public sealed record CreateLessonCommand(Guid UserId, string LessonKind, int WordCount)
    : IRequest<Result<CreateLessonResult>>;

public sealed record CreateLessonResult(long Id);

public sealed class CreateLessonCommandHandler(ILessonsDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<CreateLessonCommand, Result<CreateLessonResult>>
{
    public async Task<Result<CreateLessonResult>> Handle(
        CreateLessonCommand request,
        CancellationToken cancellationToken)
    {
        var validation = CreateLessonCommandValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<CreateLessonResult>.Failure(validation.Errors);
        }

        var lessonsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (lessonsUserId == 0)
        {
            return Result<CreateLessonResult>.Failure(new AppError(
                "lessons.create_lesson.user.not_found",
                "User was not found in the lessons module.",
                ErrorType.NotFound,
                "userId"));
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lesson = new Lesson
        {
            UserId = lessonsUserId,
            LessonKind = request.LessonKind.Trim(),
            WordCount = request.WordCount,
            StartedAtUtc = now,
            CompletedAtUtc = null
        };

        await dbContext.Lessons.AddAsync(lesson, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateLessonResult>.Success(new CreateLessonResult(lesson.Id));
    }
}
