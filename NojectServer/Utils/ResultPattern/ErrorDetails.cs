namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Contains the details describing a failed operation.
/// </summary>
/// <param name="error">The machine-readable error type or code.</param>
/// <param name="message">The human-readable error message.</param>
/// <param name="statusCode">The HTTP status code associated with the error.</param>
public sealed class ErrorDetails(string error, string message, int statusCode)
{
    /// <summary>
    /// Gets the machine-readable error type or code.
    /// </summary>
    public string Error { get; } =
        !string.IsNullOrWhiteSpace(error)
            ? error
            : throw new ArgumentException(
                "Error cannot be null or whitespace.",
                nameof(error));

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; } =
        !string.IsNullOrWhiteSpace(message)
            ? message
            : throw new ArgumentException(
                "Message cannot be null or whitespace.",
                nameof(message));

    /// <summary>
    /// Gets the HTTP status code associated with the error.
    /// </summary>
    public int StatusCode { get; } =
        statusCode is >= 100 and <= 599
            ? statusCode
            : throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "Status code must be between 100 and 599.");
}
