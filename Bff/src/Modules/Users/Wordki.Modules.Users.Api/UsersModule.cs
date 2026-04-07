using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Users.Application;
using Wordki.Modules.Users.Application.Commands.ConfirmUser;
using Wordki.Modules.Users.Application.Commands.ImpersonateUser;
using Wordki.Modules.Users.Application.Commands.LoginUser;
using Wordki.Modules.Users.Application.Commands.RegisterUser;
using Wordki.Modules.Users.Application.Commands.RemoveCurrentUser;
using Wordki.Modules.Users.Application.Queries.GetCurrentUser;
using Wordki.Modules.Users.Api.Requests;
using Wordki.Modules.Users.Api.Responses;
using Wordki.Modules.Users.Infrastructure;

namespace Wordki.Modules.Users.Api;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddUsersApplication();
        services.AddUsersInfrastructure();
        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/health", () => Results.Ok(new { module = "users", status = "ok" }));

        group.MapPost("/register", async (RegisterUserRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RegisterUserCommand(request.Email, request.Password, request.UserName),
                cancellationToken);

            if (result.IsFailure)
            {
                var statusCode = result.Errors.Any(e => e.Type == ErrorType.Conflict)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

                return Results.Json(
                    new
                    {
                        errors = result.Errors.Select(e => new
                        {
                            code = e.Code,
                            message = e.Message,
                            field = e.Field
                        })
                    },
                    statusCode: statusCode);
            }

            var payload = result.Value!;
            return Results.Created(
                $"/api/users/{payload.UserId}",
                new RegisterUserResponse(payload.UserId, payload.Email, payload.Status));
        });

        group.MapPost("/confirm", async (ConfirmUserRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ConfirmUserCommand(request.Token), cancellationToken);
            return MapConfirmUserResult(result);
        });

        group.MapGet("/confirm", async (string token, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ConfirmUserCommand(token), cancellationToken);
            return MapConfirmUserResult(result);
        });

        group.MapPost("/login", async (LoginUserRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);
            if (result.IsFailure)
            {
                var statusCode =
                    result.Errors.Any(e => e.Type == ErrorType.Unauthorized) ? StatusCodes.Status401Unauthorized :
                    result.Errors.Any(e => e.Type == ErrorType.Forbidden) ? StatusCodes.Status403Forbidden :
                    result.Errors.Any(e => e.Type == ErrorType.Conflict) ? StatusCodes.Status409Conflict :
                    StatusCodes.Status400BadRequest;

                return Results.Json(
                    new
                    {
                        errors = result.Errors.Select(e => new
                        {
                            code = e.Code,
                            message = e.Message,
                            field = e.Field
                        })
                    },
                    statusCode: statusCode);
            }

            var payload = result.Value!;
            return Results.Ok(new LoginUserResponse(
                payload.AccessToken,
                payload.TokenType,
                payload.ExpiresAtUtc,
                new CurrentUserDto(payload.UserId, payload.Email, payload.Role, payload.Status)));
        });

        group.MapPost("/impersonate", async (ImpersonateUserRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ImpersonateUserCommand(request.TargetUserId), cancellationToken);
            return Results.Ok(new ImpersonationResponse(result.EffectiveUserId, result.AccessToken, result.ExpiresAtUtc));
        });

        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
            return Results.Ok(new CurrentUserDto(result.Id, result.Email, result.Role, result.Status));
        });

        group.MapDelete("/me", async (ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RemoveCurrentUserCommand(), cancellationToken);
            return Results.NoContent();
        });
        
        return endpoints;
    }

    private static IResult MapConfirmUserResult(Result<ConfirmUserResult> result)
    {
        if (result.IsFailure)
        {
            var statusCode = result.Errors.Any(e => e.Type == ErrorType.Conflict)
                ? StatusCodes.Status409Conflict
                : result.Errors.Any(e => e.Type == ErrorType.NotFound)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

            return Results.Json(
                new
                {
                    errors = result.Errors.Select(e => new
                    {
                        code = e.Code,
                        message = e.Message,
                        field = e.Field
                    })
                },
                statusCode: statusCode);
        }

        var payload = result.Value!;
        return Results.Ok(new ConfirmUserResponse(payload.Confirmed, payload.Token));
    }
}
