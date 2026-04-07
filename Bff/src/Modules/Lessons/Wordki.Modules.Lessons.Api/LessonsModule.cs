using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Lessons.Application;
using Wordki.Modules.Lessons.Application.Commands.AddLessonRepetition;
using Wordki.Modules.Lessons.Application.Commands.CreateLesson;
using Wordki.Modules.Lessons.Infrastructure;
using Wordki.Modules.Lessons.Api.Requests;
using Wordki.Modules.Lessons.Api.Responses;

namespace Wordki.Modules.Lessons.Api;

public static class LessonsModule
{
    public static IServiceCollection AddLessonsModule(this IServiceCollection services)
    {
        services.AddLessonsApplication();
        services.AddLessonsInfrastructure();
        return services;
    }

    public static IEndpointRouteBuilder MapLessonsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/lessons").WithTags("Lessons");
        group.MapGet("/health", () => Results.Ok(new { module = "lessons", status = "ok" }));
        group.MapPost(
            "/",
            async (Guid? userId, CreateLessonRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                if (userId is null)
                {
                    return Results.Json(
                        new
                        {
                            errors = new[]
                            {
                                new
                                {
                                    code = "lessons.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new CreateLessonCommand(userId.Value, request.LessonKind, request.WordCount),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapLessonsFailure(result.Errors);
                }

                return Results.Created($"/api/lessons/{result.Value!.Id}", new CreateLessonResponse(result.Value.Id));
            });
        group.MapPost(
            "/{lessonId:long}/repetitions",
            async (
                long lessonId,
                Guid? userId,
                AddLessonRepetitionRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (userId is null)
                {
                    return Results.Json(
                        new
                        {
                            errors = new[]
                            {
                                new
                                {
                                    code = "lessons.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new AddLessonRepetitionCommand(
                        userId.Value,
                        lessonId,
                        request.QuestionResultId,
                        request.Result),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapLessonsFailure(result.Errors);
                }

                var v = result.Value!;
                return Results.Created(
                    $"/api/lessons/{lessonId}/repetitions/{v.Id}",
                    new AddLessonRepetitionResponse(v.Id, v.AnsweredAtUtc));
            });
        group.MapGet("/next", () =>
        {
            var lesson = new NextLessonCardDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Front",
                "Example front text",
                "Try to answer with back side");
            return Results.Ok(lesson);
        });
        group.MapPost(
            "/{sessionId:guid}/answer",
            (Guid sessionId, SubmitLessonAnswerRequest request, TimeProvider timeProvider, CancellationToken cancellationToken) =>
            {
                return Results.Ok(new SubmitLessonAnswerResponse(
                    sessionId,
                    request.CardId,
                    request.IsCorrect,
                    timeProvider.GetUtcNow().UtcDateTime));
            });
        return endpoints;
    }

    private static IResult MapLessonsFailure(IReadOnlyList<AppError> errors)
    {
        var statusCode = errors.Any(e => e.Type == ErrorType.NotFound)
            ? StatusCodes.Status404NotFound
            : errors.Any(e => e.Type == ErrorType.Forbidden)
                ? StatusCodes.Status403Forbidden
                : errors.Any(e => e.Type == ErrorType.Conflict)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

        return Results.Json(
            new
            {
                errors = errors.Select(e => new
                {
                    code = e.Code,
                    message = e.Message,
                    field = e.Field
                })
            },
            statusCode: statusCode);
    }
}
