namespace NojectServer.Utils;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; } // The result if successful
    public string? Error { get; } // The error message if failed

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
