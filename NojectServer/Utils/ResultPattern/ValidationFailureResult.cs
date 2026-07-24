namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Represents a failed result caused by invalid input fields.
/// </summary>
/// <typeparam name="T">
/// The type of value that would have been returned on success.
/// </typeparam>
public sealed class ValidationFailureResult<T> : Result<T>
{
    private const int ValidationStatusCode = 400;

    public ValidationFailureResult(
        IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "At least one validation error is required.",
                nameof(errors));
        }

        ValidationErrors = errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray());

        Error = new ErrorDetails(
            "Validation",
            "One or more validation errors occurred.",
            ValidationStatusCode);
    }

    public override bool IsSuccess => false;

    /// <summary>
    /// Gets the general validation error information.
    /// </summary>
    public ErrorDetails Error { get; }

    /// <summary>
    /// Gets validation errors grouped by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}
