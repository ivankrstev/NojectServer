namespace NojectServer.Utils;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; } // The result if successful
    public ErrorDetails? Error { get; } // The error message if failed

    // Constructor is private to prevent creating instances directly
    private Result(bool isSuccess, T? value, ErrorDetails? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    // Factory methods to create instances
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string errorType, string errorMessage) =>
    new(false, default, new ErrorDetails(errorType, errorMessage, 400));
    public static Result<T> Failure(string errorType, string errorMessage, int statusCode) =>
        new(false, default, new ErrorDetails(errorType, errorMessage, statusCode));
}
