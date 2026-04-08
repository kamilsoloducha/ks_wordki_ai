using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Commands.TickCardResult;

public static class TickCardResultCommandValidator
{
    public static Result Validate(TickCardResultCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (command.ResultId < 1)
        {
            return Result.Failure(new AppError(
                "cards.validation.result_id.invalid",
                "Result id must be a positive number.",
                ErrorType.Validation,
                "resultId"));
        }

        return Result.Success();
    }
}
