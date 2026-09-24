namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Represents a failed operation without a return value.
/// </summary>
/// <param name="error">The details describing the failure.</param>
public sealed class FailureResult(ErrorDetails error) : Result
{
    /// <inheritdoc />
    public override bool IsSuccess => false;

    /// <inheritdoc />
    public override ErrorDetails Error { get; } =
        error ?? throw new ArgumentNullException(nameof(error));
}

/// <summary>
/// Represents a failed operation that would have returned a value on success.
/// </summary>
/// <typeparam name="T">
/// The type of value that would have been returned on success.
/// </typeparam>
/// <param name="error">The details describing the failure.</param>
public sealed class FailureResult<T>(ErrorDetails error) : Result<T>
{
    /// <inheritdoc />
    public override bool IsSuccess => false;

    /// <inheritdoc />
    public override ErrorDetails Error { get; } =
        error ?? throw new ArgumentNullException(nameof(error));
}
