namespace Wordki.Bff.SharedKernel.Results;

public sealed class Result
{
    private Result(bool isSuccess, IReadOnlyList<AppError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<AppError> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(params AppError[] errors) => new(false, errors);

    public static Result Failure(IReadOnlyList<AppError> errors) => new(false, errors);
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, IReadOnlyList<AppError> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public IReadOnlyList<AppError> Errors { get; }

    public static Result<T> Success(T value) => new(true, value, []);

    public static Result<T> Failure(params AppError[] errors) => new(false, default, errors);

    public static Result<T> Failure(IReadOnlyList<AppError> errors) => new(false, default, errors);
}
