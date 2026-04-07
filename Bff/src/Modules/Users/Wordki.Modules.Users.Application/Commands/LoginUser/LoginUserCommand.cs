using MediatR;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Users.Application.Abstractions;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Application.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<Result<LoginUserResult>>;

public sealed record LoginUserResult(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string Role,
    string Status);

public sealed class LoginUserCommandHandler(
    IUsersDbContext dbContext,
    IPasswordHasher passwordHasher,
    IBearerTokenService bearerTokenService,
    TimeProvider timeProvider) : IRequestHandler<LoginUserCommand, Result<LoginUserResult>>
{
    public async Task<Result<LoginUserResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = LoginUserCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<LoginUserResult>.Failure(validationResult.Errors);
        }

        var normalizedEmail = NormalizedEmail.Create(request.Email);
        var user = await dbContext.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return Result<LoginUserResult>.Failure(new AppError(
                "users.login.invalid_credentials",
                "Email or password is invalid.",
                ErrorType.Unauthorized));
        }

        var hashedPassword = passwordHasher.HashPassword(request.Password);
        if (!string.Equals(user.PasswordHash, hashedPassword, StringComparison.Ordinal))
        {
            return Result<LoginUserResult>.Failure(new AppError(
                "users.login.invalid_credentials",
                "Email or password is invalid.",
                ErrorType.Unauthorized));
        }

        if (user.Status == UserStatus.PendingConfirmation)
        {
            return Result<LoginUserResult>.Failure(new AppError(
                "users.login.email_not_confirmed",
                "Email is not confirmed.",
                ErrorType.Conflict,
                "email"));
        }

        if (user.Status == UserStatus.Blocked)
        {
            return Result<LoginUserResult>.Failure(new AppError(
                "users.login.user_blocked",
                "User account is blocked.",
                ErrorType.Forbidden));
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        user.LastLoginAtUtc = nowUtc;
        user.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = bearerTokenService.CreateToken(new BearerTokenPayload(
            user.Id,
            user.Email,
            user.Role.ToString()));

        var result = new LoginUserResult(
            accessToken,
            "Bearer",
            timeProvider.GetUtcNow().AddHours(1).UtcDateTime,
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Status.ToString());

        return Result<LoginUserResult>.Success(result);
    }
}
