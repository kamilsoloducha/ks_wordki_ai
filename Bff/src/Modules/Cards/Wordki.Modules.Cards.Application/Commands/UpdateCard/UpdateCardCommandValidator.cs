using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Commands.UpdateCard;

public static class UpdateCardCommandValidator
{
    private const int LabelMaxLength = 200;
    private const int ExampleMaxLength = 1000;
    private const int CommentMaxLength = 1000;

    public static Result Validate(UpdateCardCommand command)
    {
        var errors = new List<AppError>();

        if (command.UserId == Guid.Empty)
        {
            errors.Add(new AppError(
                "cards.validation.user_id.required",
                "User id is required.",
                ErrorType.Validation,
                "userId"));
        }

        if (command.CardId <= 0)
        {
            errors.Add(new AppError(
                "cards.validation.card_id.invalid",
                "Card id must be a positive number.",
                ErrorType.Validation,
                "cardId"));
        }

        ValidateSide(
            errors,
            command.FrontLabel,
            command.FrontExample,
            command.FrontComment,
            "front.label",
            "front.example",
            "front.comment");

        ValidateSide(
            errors,
            command.BackLabel,
            command.BackExample,
            command.BackComment,
            "back.label",
            "back.example",
            "back.comment");

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(errors);
    }

    private static void ValidateSide(
        List<AppError> errors,
        string label,
        string? example,
        string? comment,
        string labelField,
        string exampleField,
        string commentField)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(new AppError(
                "cards.validation.label.required",
                "Label is required.",
                ErrorType.Validation,
                labelField));
        }
        else if (label.Trim().Length > LabelMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.label.too_long",
                $"Label cannot exceed {LabelMaxLength} characters.",
                ErrorType.Validation,
                labelField));
        }

        if (!string.IsNullOrWhiteSpace(example) && example.Trim().Length > ExampleMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.example.too_long",
                $"Example cannot exceed {ExampleMaxLength} characters.",
                ErrorType.Validation,
                exampleField));
        }

        if (!string.IsNullOrWhiteSpace(comment) && comment.Trim().Length > CommentMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.comment.too_long",
                $"Comment cannot exceed {CommentMaxLength} characters.",
                ErrorType.Validation,
                commentField));
        }
    }
}
