namespace NojectServer.Utils.ResultPattern;

public abstract class Result<T>
{
    public abstract bool IsSuccess { get; }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new SuccessResult<T>(value);

    public static Result<T> Failure<T>(string errorType, string errorMessage) => new FailureResult<T>(new ErrorDetails(errorType, errorMessage, 400));

    public static Result<T> Failure<T>(string errorType, string errorMessage, int statusCode) =>
        new FailureResult<T>(new ErrorDetails(errorType, errorMessage, statusCode));
}
