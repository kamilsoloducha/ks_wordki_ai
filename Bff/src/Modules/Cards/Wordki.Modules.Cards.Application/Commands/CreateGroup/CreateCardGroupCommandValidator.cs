using Wordki.Bff.SharedKernel.Results;

namespace Wordki.Modules.Cards.Application.Commands.CreateGroup;

public static class CreateCardGroupCommandValidator
{
    private const int NameMaxLength = 200;
    private const int SideTypeMaxLength = 100;

    public static Result Validate(CreateCardGroupCommand command)
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

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add(new AppError(
                "cards.validation.name.required",
                "Name is required.",
                ErrorType.Validation,
                "name"));
        }
        else if (command.Name.Trim().Length > NameMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.name.too_long",
                $"Name cannot exceed {NameMaxLength} characters.",
                ErrorType.Validation,
                "name"));
        }

        if (string.IsNullOrWhiteSpace(command.FrontSideType))
        {
            errors.Add(new AppError(
                "cards.validation.front_side_type.required",
                "Front side type is required.",
                ErrorType.Validation,
                "frontSideType"));
        }
        else if (command.FrontSideType.Trim().Length > SideTypeMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.front_side_type.too_long",
                $"Front side type cannot exceed {SideTypeMaxLength} characters.",
                ErrorType.Validation,
                "frontSideType"));
        }

        if (string.IsNullOrWhiteSpace(command.BackSideType))
        {
            errors.Add(new AppError(
                "cards.validation.back_side_type.required",
                "Back side type is required.",
                ErrorType.Validation,
                "backSideType"));
        }
        else if (command.BackSideType.Trim().Length > SideTypeMaxLength)
        {
            errors.Add(new AppError(
                "cards.validation.back_side_type.too_long",
                $"Back side type cannot exceed {SideTypeMaxLength} characters.",
                ErrorType.Validation,
                "backSideType"));
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(errors);
    }
}
