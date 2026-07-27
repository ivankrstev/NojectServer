namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Represents an operation that can succeed or fail without returning a value.
/// </summary>
public abstract class Result
{
    private const int DefaultFailureStatusCode = 400;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public abstract bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error details when the operation failed;
    /// otherwise, <see langword="null"/>.
    /// </summary>
    public abstract ErrorDetails? Error { get; }

    /// <summary>
    /// Creates a successful result without a return value.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() =>
        new SuccessResult();

    /// <summary>
    /// Creates a failed result without a return value.
    /// </summary>
    /// <param name="errorDetails">
    /// The error details describing the failure.
    /// </param>
    /// <returns>A failed result.</returns>
    public static Result Failure(ErrorDetails errorDetails) =>
        new FailureResult(errorDetails);

    /// <summary>
    /// Creates a failed result using the default status code of 400.
    /// </summary>
    /// <param name="errorType">The error type or code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(
        string errorType,
        string errorMessage) =>
        Failure(
            errorType,
            errorMessage,
            DefaultFailureStatusCode);

    /// <summary>
    /// Creates a failed result with the specified status code.
    /// </summary>
    /// <param name="errorType">The error type or code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="statusCode">
    /// The HTTP status code associated with the failure.
    /// </param>
    /// <returns>A failed result.</returns>
    public static Result Failure(
        string errorType,
        string errorMessage,
        int statusCode) =>
        new FailureResult(
            new ErrorDetails(
                errorType,
                errorMessage,
                statusCode));

    /// <summary>
    /// Creates a failed result containing field-specific validation errors.
    /// </summary>
    /// <param name="validationErrors">
    /// The validation errors grouped by property name.
    /// </param>
    /// <returns>A validation failure result.</returns>
    public static Result ValidationFailure(
        IReadOnlyDictionary<string, string[]> errors) =>
        new ValidationFailureResult(errors);

    /// <summary>
    /// Creates a successful result containing the specified value.
    /// </summary>
    /// <typeparam name="T">The successful result value type.</typeparam>
    /// <param name="value">The successful result value.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<T> Success<T>(T value) =>
        new SuccessResult<T>(value);

    /// <summary>
    /// Creates a failed result for an operation that would return a value.
    /// </summary>
    /// <typeparam name="T">The successful result value type.</typeparam>
    /// <param name="errorDetails">
    /// The error details describing the failure.
    /// </param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(ErrorDetails errorDetails) =>
        new FailureResult<T>(errorDetails);

    /// <summary>
    /// Creates a failed result using the default status code of 400.
    /// </summary>
    /// <typeparam name="T">The successful result value type.</typeparam>
    /// <param name="errorType">The error type or code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(
        string errorType,
        string errorMessage) =>
        Failure<T>(
            errorType,
            errorMessage,
            DefaultFailureStatusCode);

    /// <summary>
    /// Creates a failed result with the specified status code.
    /// </summary>
    /// <typeparam name="T">The successful result value type.</typeparam>
    /// <param name="errorType">The error type or code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="statusCode">
    /// The HTTP status code associated with the failure.
    /// </param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(
        string errorType,
        string errorMessage,
        int statusCode) =>
        new FailureResult<T>(
            new ErrorDetails(
                errorType,
                errorMessage,
                statusCode));

    /// <summary>
    /// Creates a failed result containing field-specific validation errors.
    /// </summary>
    /// <typeparam name="T">The successful result value type.</typeparam>
    /// <param name="validationErrors">
    /// The validation errors grouped by property name.
    /// </param>
    /// <returns>A validation failure result.</returns>
    public static Result<T> ValidationFailure<T>(
        IReadOnlyDictionary<string, string[]> errors) =>
        new ValidationFailureResult<T>(errors);
}

/// <summary>
/// Represents an operation that can succeed or fail and contains a value
/// when successful.
/// </summary>
/// <typeparam name="T">The successful result value type.</typeparam>
public abstract class Result<T> : Result;
