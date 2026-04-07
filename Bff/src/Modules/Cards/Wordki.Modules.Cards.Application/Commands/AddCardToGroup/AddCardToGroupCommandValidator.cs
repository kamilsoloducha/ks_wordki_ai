using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Commands.AddCardToGroup;

public static class AddCardToGroupCommandValidator
{
    private const int LabelMaxLength = 200;
    private const int ExampleMaxLength = 1000;
    private const int CommentMaxLength = 1000;

    public static Result Validate(AddCardToGroupCommand command)
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

        if (command.GroupId <= 0)
        {
            errors.Add(new AppError(
                "cards.validation.group_id.invalid",
                "Group id must be a positive number.",
                ErrorType.Validation,
                "groupId"));
        }

        if (string.IsNullOrWhiteSpace(command.FrontLabel))
        {
            errors.Add(new AppError(
                "cards.validation.front_label.required",
                "Front label is required.",
                ErrorType.Validation,
                "frontLabel"));
        }
        else if (command.FrontLabel.Trim().Length > LabelMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.front_label.too_long",
                $"Front label cannot exceed {LabelMaxLength} characters.",
                ErrorType.Validation,
                "frontLabel"));
        }

        if (string.IsNullOrWhiteSpace(command.BackLabel))
        {
            errors.Add(new AppError(
                "cards.validation.back_label.required",
                "Back label is required.",
                ErrorType.Validation,
                "backLabel"));
        }
        else if (command.BackLabel.Trim().Length > LabelMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.back_label.too_long",
                $"Back label cannot exceed {LabelMaxLength} characters.",
                ErrorType.Validation,
                "backLabel"));
        }

        ValidateOptionalText(
            errors,
            command.FrontExample,
            "frontExample",
            "cards.validation.front_example.too_long",
            ExampleMaxLength);

        ValidateOptionalText(
            errors,
            command.FrontComment,
            "frontComment",
            "cards.validation.front_comment.too_long",
            CommentMaxLength);

        ValidateOptionalText(
            errors,
            command.BackExample,
            "backExample",
            "cards.validation.back_example.too_long",
            ExampleMaxLength);

        ValidateOptionalText(
            errors,
            command.BackComment,
            "backComment",
            "cards.validation.back_comment.too_long",
            CommentMaxLength);

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(errors);
    }

    private static void ValidateOptionalText(
        List<AppError> errors,
        string? value,
        string field,
        string tooLongCode,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(new AppError(
                tooLongCode,
                $"Value cannot exceed {maxLength} characters.",
                ErrorType.Validation,
                field));
        }
    }
}
