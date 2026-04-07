using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Users.Application.Commands.ConfirmUser;

public static class ConfirmUserCommandValidator
{
    public static Result Validate(ConfirmUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure(new AppError(
                "users.validation.token.required",
                "Confirmation token is required.",
                ErrorType.Validation,
                "token"));
        }

        return Result.Success();
    }
}
