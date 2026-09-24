namespace NojectServer.Utils.ResultPattern;

/// <summary>
/// Represents a successful operation without a return value.
/// </summary>
public sealed class SuccessResult : Result
{
    /// <inheritdoc />
    public override bool IsSuccess => true;

    /// <inheritdoc />
    public override ErrorDetails? Error => null;
}

/// <summary>
/// Represents a successful operation containing a return value.
/// </summary>
/// <typeparam name="T">The type of the successful value.</typeparam>
/// <param name="value">The successful operation value.</param>
public sealed class SuccessResult<T>(T value) : Result<T>
{
    /// <inheritdoc />
    public override bool IsSuccess => true;

    /// <inheritdoc />
    public override ErrorDetails? Error => null;

    /// <summary>
    /// Gets the value produced by the successful operation.
    /// </summary>
    public T Value { get; } =
        value ?? throw new ArgumentNullException(
            nameof(value),
            "Value cannot be null in a success result.");
}
