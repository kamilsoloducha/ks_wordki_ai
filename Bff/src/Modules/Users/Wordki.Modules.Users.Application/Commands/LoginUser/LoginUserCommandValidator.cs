using System.Text.RegularExpressions;
using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Users.Application.Commands.LoginUser;

public static partial class LoginUserCommandValidator
{
    public static Result Validate(LoginUserCommand command)
    {
        var errors = new List<AppError>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add(new AppError(
                "users.validation.email.required",
                "Email is required.",
                ErrorType.Validation,
                "email"));
        }

        if (!string.IsNullOrWhiteSpace(command.Email) && !EmailRegex().IsMatch(command.Email.Trim()))
        {
            errors.Add(new AppError(
                "users.validation.email.invalid",
                "Email format is invalid.",
                ErrorType.Validation,
                "email"));
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add(new AppError(
                "users.validation.password.required",
                "Password is required.",
                ErrorType.Validation,
                "password"));
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(errors);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
