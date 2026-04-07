using MediatR;
using Microsoft.Extensions.Options;
using Wordki.Bff.SharedKernel.Abstractions;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Users.Application.Abstractions;
using Wordki.Modules.Users.Application.IntegrationEvents;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Application.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, string UserName) : IRequest<Result<RegisterUserResult>>;

public sealed record RegisterUserResult(Guid UserId, string Email, string Status);

public sealed class RegisterUserCommandHandler(
    IUsersDbContext dbContext,
    IPasswordHasher passwordHasher,
    IConfirmationTokenHasher confirmationTokenHasher,
    IEmailSender emailSender,
    IOptions<EmailConfirmationOptions> emailConfirmationOptions,
    TimeProvider timeProvider) : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = RegisterUserCommandValidator.Validate(request);
        if (validationResult.IsFailure)
        {
            return Result<RegisterUserResult>.Failure(validationResult.Errors);
        }

        var normalizedEmail = NormalizedEmail.Create(request.Email);
        var emailExists = await dbContext.NormalizedEmailExistsAsync(normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result<RegisterUserResult>.Failure(new AppError(
                "users.register.email.already_exists",
                "User with the same email already exists.",
                ErrorType.Conflict,
                "email"));
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var confirmationToken = Guid.NewGuid().ToString("N");
        var confirmationTokenHash = confirmationTokenHasher.Hash(confirmationToken);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            UserName = request.UserName.Trim(),
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Role = UserRole.User,
            Status = UserStatus.PendingConfirmation,
            EmailConfirmationTokenHash = confirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = nowUtc.AddDays(1),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.AddUserAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var confirmationLink = BuildConfirmationLink(
                emailConfirmationOptions.Value.ConfirmationUrlBase,
                confirmationToken);

            await emailSender.SendAsync(
                user.Email,
                "Confirm your Wordki account",
                $"Click the link to confirm your email: {confirmationLink}",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Result<RegisterUserResult>.Success(new RegisterUserResult(user.Id, user.Email, user.Status.ToString()));
    }

    private static string BuildConfirmationLink(string baseUrl, string token)
    {
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
    }
}
