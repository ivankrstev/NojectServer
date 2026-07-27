namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Represents a payload-free result that failed because of invalid input fields.
/// </summary>
public sealed class ValidationFailureResult : Result
{
    public ValidationFailureResult(
        IReadOnlyDictionary<string, string[]> validationErrors)
    {
        (Error, ValidationErrors) =
            ValidationFailureData.Create(validationErrors);
    }

    public override bool IsSuccess => false;

    /// <summary>
    /// Gets the general details describing the validation failure.
    /// </summary>
    public override ErrorDetails Error { get; }

    /// <summary>
    /// Gets the validation errors grouped by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}

/// <summary>
/// Represents a value-returning result that failed because of invalid input fields.
/// </summary>
public sealed class ValidationFailureResult<T> : Result<T>
{
    public ValidationFailureResult(
        IReadOnlyDictionary<string, string[]> validationErrors)
    {
        (Error, ValidationErrors) =
            ValidationFailureData.Create(validationErrors);
    }

    public override bool IsSuccess => false;

    /// <summary>
    /// Gets the general details describing the validation failure.
    /// </summary>
    public override ErrorDetails Error { get; }

    /// <summary>
    /// Gets the validation errors grouped by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}

/// <summary>
/// Creates shared error data for validation failure results.
/// </summary>
internal static class ValidationFailureData
{
    private const int ValidationStatusCode = 400;

    /// <summary>
    /// Creates general error details and a defensive copy of the
    /// field-specific validation errors.
    /// </summary>
    /// <param name="validationErrors">
    /// The validation errors grouped by property name.
    /// </param>
    /// <returns>
    /// The general validation error and copied field-specific errors.
    /// </returns>
    public static (
        ErrorDetails Error,
        IReadOnlyDictionary<string, string[]> ValidationErrors)
        Create(IReadOnlyDictionary<string, string[]> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        if (validationErrors.Count == 0)
        {
            throw new ArgumentException(
                "At least one validation error is required.",
                nameof(validationErrors));
        }

        IReadOnlyDictionary<string, string[]> copiedValidationErrors =
            validationErrors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value?.ToArray()
                    ?? throw new ArgumentException(
                        "A validation error collection cannot be null.",
                        nameof(validationErrors)));

        var error = new ErrorDetails(
            "Validation",
            "One or more validation errors occurred.",
            ValidationStatusCode);

        return (error, copiedValidationErrors);
    }
}
