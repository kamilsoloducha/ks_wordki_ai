namespace Wordki.Bff.SharedKernel.Results;

public sealed record AppError(
    string Code,
    string Message,
    ErrorType Type,
    string? Field = null);

public enum ErrorType
{
    Validation = 1,
    Conflict = 2,
    NotFound = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Unexpected = 6
}
