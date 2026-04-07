using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Commands.DeleteCard;

public static class DeleteCardCommandValidator
{
    public static Result Validate(DeleteCardCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Result.Failure(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (command.CardId < 1)
        {
            return Result.Failure(new AppError(
                "cards.validation.card_id.invalid",
                "Card id must be a positive number.",
                ErrorType.Validation,
                "cardId"));
        }

        return Result.Success();
    }
}
