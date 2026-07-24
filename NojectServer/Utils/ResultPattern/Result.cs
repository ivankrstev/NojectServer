namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Represents the common result contract for operations that can succeed or fail.
/// </summary>
public abstract class Result<T>
{
    public abstract bool IsSuccess { get; }
}

/// <summary>
/// Factory helpers for creating successful or failed results.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value of the successful result.</param>
    /// <returns>A successful result containing the specified value.</returns>
    public static Result<T> Success<T>(T value) => new SuccessResult<T>(value);

    /// <summary>
    /// Creates a failed result with the specified error details.
    /// </summary>
    /// <typeparam name="T">The type of the value that would have been returned on success.</typeparam>
    /// <param name="errorDetails">The error details describing the failure.</param>
    /// <returns>A failed result containing the specified error details.</returns>
    public static Result<T> Failure<T>(ErrorDetails errorDetails) => new FailureResult<T>(errorDetails);

    /// <summary>
    /// Creates a failed result with the specified error type and message, using a default status code of 400.
    /// </summary>
    /// <typeparam name="T">The type of the value that would have been returned on success.</typeparam>
    /// <param name="errorType">The type of the error.</param>
    /// <param name="errorMessage">The message describing the error.</param>
    /// <returns>A failed result containing the specified error details.</returns>
    public static Result<T> Failure<T>(string errorType, string errorMessage) => new FailureResult<T>(new ErrorDetails(errorType, errorMessage, 400));

    /// <summary>
    /// Creates a failed result with the specified error type, message, and status code.
    /// </summary>
    /// <typeparam name="T">The type of the value that would have been returned on success.</typeparam>
    /// <param name="errorType">The type of the error.</param>
    /// <param name="errorMessage">The message describing the error.</param>
    /// <param name="statusCode">The status code for the error.</param>
    /// <returns>A failed result containing the specified error details.</returns>
    public static Result<T> Failure<T>(string errorType, string errorMessage, int statusCode) =>
        new FailureResult<T>(new ErrorDetails(errorType, errorMessage, statusCode));

    /// <summary>
    /// Creates a failed result containing field-specific validation errors.
    /// </summary>
    /// <typeparam name="T">The type of the value that would have been returned on success.</typeparam>
    /// <param name="errors">The validation errors grouped by property name.</param>
    /// <returns>A failed result containing the specified validation errors.</returns>
    public static Result<T> ValidationFailure<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new ValidationFailureResult<T>(errors);
}
