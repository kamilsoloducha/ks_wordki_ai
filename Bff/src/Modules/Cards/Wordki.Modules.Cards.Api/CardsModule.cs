using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application;
using Wordki.Modules.Cards.Api.Requests;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Application.Commands.AddCardToGroup;
using Wordki.Modules.Cards.Application.Commands.CreateGroup;
using Wordki.Modules.Cards.Application.Commands.DeleteCard;
using Wordki.Modules.Cards.Application.Commands.UpdateCard;
using Wordki.Modules.Cards.Application.Commands.UpdateCardGroup;
using Wordki.Modules.Cards.Application.Queries.GetGroupCards;
using Wordki.Modules.Cards.Application.Queries.GetDistinctSideTypePairs;
using Wordki.Modules.Cards.Application.Queries.GetUserCardGroups;
using Wordki.Modules.Cards.Application.Queries.GetUserWordCount;
using Wordki.Modules.Cards.Application.Queries.GetDueTodayCards;
using Wordki.Modules.Cards.Application.Queries.GetLessonWords;
using Wordki.Modules.Cards.Application.Queries;
using Wordki.Modules.Cards.Application.Queries.GetWordsDueTodayCount;
using Wordki.Modules.Cards.Application.Queries.SearchCards;
using Wordki.Modules.Cards.Infrastructure;

namespace Wordki.Modules.Cards.Api;

public static class CardsModule
{
    public static IServiceCollection AddCardsModule(this IServiceCollection services)
    {
        services.AddCardsApplication();
        services.AddCardsInfrastructure();
        return services;
    }

    public static IEndpointRouteBuilder MapCardsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/cards").WithTags("Cards");
        group.MapGet("/health", () => Results.Ok(new { module = "cards", status = "ok" }));
        group.MapGet("/groups", async (Guid? userId, ISender sender, CancellationToken cancellationToken) =>
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
                                code = "cards.validation.user_id.required",
                                message = "User id is required.",
                                field = (string?)"userId"
                            }
                        }
                    },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await sender.Send(new GetUserCardGroupsQuery(userId.Value), cancellationToken);

            if (result.IsFailure)
            {
                return MapCardsFailure(result.Errors);
            }

            var payload = result.Value!.Select(x => new UserCardGroupDto(
                x.Id,
                x.Name,
                x.FrontSideType,
                x.BackSideType,
                x.CardCount)).ToList();

            return Results.Ok(payload);
        });
        group.MapGet(
            "/side-type-pairs",
            async (Guid? userId, ISender sender, CancellationToken cancellationToken) =>
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new GetDistinctSideTypePairsQuery(userId.Value),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                var payload = result.Value!
                    .Select(x => new SideTypePairDto(x.SideType1, x.SideType2))
                    .ToList();

                return Results.Ok(payload);
            });
        group.MapGet(
            "/words-count",
            async (Guid? userId, ISender sender, CancellationToken cancellationToken) =>
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(new GetUserWordCountQuery(userId.Value), cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                return Results.Ok(new UserWordCountDto(result.Value));
            });
        group.MapGet(
            "/due-today-count",
            async (
                Guid? userId,
                string? questionSideType,
                string? answerSideType,
                string? wordSource,
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new GetWordsDueTodayCountQuery(
                        userId.Value,
                        questionSideType,
                        answerSideType,
                        LessonWordSourceParser.Parse(wordSource)),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                return Results.Ok(new WordsDueTodayCountDto(result.Value));
            });
        group.MapGet(
            "/due-today",
            async (
                Guid? userId,
                string? questionSideType,
                string? answerSideType,
                int? limit,
                string? wordSource,
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var effectiveLimit = limit ?? 20;

                var result = await sender.Send(
                    new GetDueTodayCardsQuery(
                        userId.Value,
                        questionSideType,
                        answerSideType,
                        effectiveLimit,
                        LessonWordSourceParser.Parse(wordSource)),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                var payload = result.Value!
                    .Select(x => new CardDto(
                        x.Id,
                        x.GroupId,
                        new CardSideDto(x.FrontLabel, x.FrontExample, x.FrontComment),
                        new CardSideDto(x.BackLabel, x.BackExample, x.BackComment),
                        x.QuestionResultId))
                    .ToList();

                return Results.Ok(payload);
            });
        group.MapGet(
            "/groups/{groupId:long}/lesson-words",
            async (
                long groupId,
                Guid? userId,
                string? questionSideType,
                string? answerSideType,
                int? limit,
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var effectiveLimit = limit ?? 20;

                var result = await sender.Send(
                    new GetLessonWordsQuery(
                        userId.Value,
                        groupId,
                        questionSideType ?? string.Empty,
                        answerSideType ?? string.Empty,
                        effectiveLimit),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                var payload = result.Value!
                    .Select(x => new LessonWordDto(
                        x.QuestionResultId,
                        x.QuestionDrawer,
                        x.QuestionLabel,
                        x.QuestionExample,
                        x.AnswerLabel,
                        x.AnswerExample))
                    .ToList();

                return Results.Ok(payload);
            });
        group.MapGet(
            "/groups/{groupId:long}/cards",
            async (long groupId, Guid? userId, ISender sender, CancellationToken cancellationToken) =>
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new GetGroupCardsQuery(userId.Value, groupId),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                var payload = result.Value!
                    .Select(x => new CardDto(
                        x.Id,
                        x.GroupId,
                        new CardSideDto(x.FrontLabel, x.FrontExample, x.FrontComment),
                        new CardSideDto(x.BackLabel, x.BackExample, x.BackComment)))
                    .ToList();

                return Results.Ok(payload);
            });
        group.MapPost("/groups", async (CreateCardGroupRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateCardGroupCommand(request.UserId, request.Name, request.FrontSideType, request.BackSideType),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapCardsFailure(result.Errors);
            }

            var groupPayload = result.Value!;
            return Results.Created(
                $"/api/cards/groups/{groupPayload.Id}",
                new CardGroupDto(groupPayload.Id, groupPayload.Name, groupPayload.FrontSideType, groupPayload.BackSideType));
        });
        group.MapPost("/cards", async (AddCardToGroupRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new AddCardToGroupCommand(
                    request.UserId,
                    request.GroupId,
                    request.FrontLabel,
                    request.BackLabel,
                    request.FrontExample,
                    request.FrontComment,
                    request.BackExample,
                    request.BackComment),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapCardsFailure(result.Errors);
            }

            var payload = result.Value!;
            return Results.Created(
                $"/api/cards/{payload.Id}",
                new CardDto(
                    payload.Id,
                    payload.GroupId,
                    new CardSideDto(payload.Front.Label, payload.Front.Example, payload.Front.Comment),
                    new CardSideDto(payload.Back.Label, payload.Back.Example, payload.Back.Comment)));
        });
        group.MapGet(
            "/search",
            async (
                Guid? userId,
                int? drawer,
                long? groupId,
                bool? getCount,
                bool? getList,
                int? page,
                int? pageSize,
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new SearchCardsQuery(
                        userId.Value,
                        drawer,
                        groupId,
                        getCount ?? false,
                        getList ?? false,
                        page ?? 1,
                        pageSize ?? 50),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                var value = result.Value!;

                if (value.Count is int c && value.Items is not null)
                {
                    var items = value.Items
                        .Select(x => new CardDto(
                            x.Id,
                            x.GroupId,
                            new CardSideDto(x.FrontLabel, x.FrontExample, x.FrontComment),
                            new CardSideDto(x.BackLabel, x.BackExample, x.BackComment)))
                        .ToList();
                    return Results.Ok(new SearchCardsWithCountDto(c, items));
                }

                if (value.Count is int countOnly)
                {
                    return Results.Ok(new SearchCardsCountDto(countOnly));
                }

                var list = value.Items!
                    .Select(x => new CardDto(
                        x.Id,
                        x.GroupId,
                        new CardSideDto(x.FrontLabel, x.FrontExample, x.FrontComment),
                        new CardSideDto(x.BackLabel, x.BackExample, x.BackComment)))
                    .ToList();

                return Results.Ok(list);
            });
        group.MapPatch("/groups/{id:long}", async (long id, UpdateCardGroupRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateCardGroupCommand(
                    request.UserId,
                    id,
                    request.Name,
                    request.FrontSideType,
                    request.BackSideType),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapCardsFailure(result.Errors);
            }

            var payload = result.Value!;
            return Results.Ok(
                new CardGroupDto(payload.Id, payload.Name, payload.FrontSideType, payload.BackSideType));
        });
        group.MapPatch("/{id:long}", async (long id, UpdateCardRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateCardCommand(
                    request.UserId,
                    id,
                    request.Front.Label,
                    request.Back.Label,
                    request.Front.Example,
                    request.Front.Comment,
                    request.Back.Example,
                    request.Back.Comment),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapCardsFailure(result.Errors);
            }

            var payload = result.Value!;
            return Results.Ok(
                new CardDto(
                    payload.Id,
                    payload.GroupId,
                    new CardSideDto(payload.Front.Label, payload.Front.Example, payload.Front.Comment),
                    new CardSideDto(payload.Back.Label, payload.Back.Example, payload.Back.Comment)));
        });
        group.MapDelete(
            "/{id:long}",
            async (long id, Guid? userId, ISender sender, CancellationToken cancellationToken) =>
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
                                    code = "cards.validation.user_id.required",
                                    message = "User id is required.",
                                    field = (string?)"userId"
                                }
                            }
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new DeleteCardCommand(userId.Value, id),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return MapCardsFailure(result.Errors);
                }

                return Results.NoContent();
            });
        return endpoints;
    }

    private static IResult MapCardsFailure(IReadOnlyList<AppError> errors)
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
