using MediatR;
using System.Text.Json;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Users.Application.Abstractions;
using Wordki.Modules.Users.Domain.Events;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Application.Commands.ConfirmUser;

public sealed record ConfirmUserCommand(string Token) : IRequest<Result<ConfirmUserResult>>;

public sealed record ConfirmUserResult(bool Confirmed, string Token);

public sealed class ConfirmUserCommandHandler(
    IUsersDbContext dbContext,
    IConfirmationTokenHasher confirmationTokenHasher,
    TimeProvider timeProvider) : IRequestHandler<ConfirmUserCommand, Result<ConfirmUserResult>>
{
    public async Task<Result<ConfirmUserResult>> Handle(ConfirmUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = ConfirmUserCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<ConfirmUserResult>.Failure(validationResult.Errors);
        }

        var tokenHash = confirmationTokenHasher.Hash(request.Token.Trim());
        var user = await dbContext.GetByEmailConfirmationTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
        {
            return Result<ConfirmUserResult>.Failure(new AppError(
                "users.confirm.token.invalid",
                "Confirmation token is invalid.",
                ErrorType.NotFound,
                "token"));
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (user.EmailConfirmationTokenExpiresAtUtc.HasValue &&
            user.EmailConfirmationTokenExpiresAtUtc.Value < nowUtc)
        {
            return Result<ConfirmUserResult>.Failure(new AppError(
                "users.confirm.token.expired",
                "Confirmation token has expired.",
                ErrorType.Conflict,
                "token"));
        }

        user.Status = UserStatus.Active;
        user.EmailConfirmedAtUtc = nowUtc;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = nowUtc;

        var userConfirmedEvent = new UserConfirmed
        {
            Id = user.Id
        };

        var payload = JsonSerializer.Serialize(userConfirmedEvent);
        await dbContext.AddSharedEventMessagesAsync(
            [
                new SharedEventMessage
                {
                    PublisherName = "Users",
                    ConsumerName = OutboxRouting.Broadcast,
                    DataType = "UserConfirmed",
                    AddedAtUtc = nowUtc,
                    Payload = payload
                }
            ],
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ConfirmUserResult>.Success(new ConfirmUserResult(true, request.Token));
    }
}
