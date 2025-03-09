namespace NojectServer.Utils.ResultPattern;

public sealed class FailureResult<T>(ErrorDetails error) : Result<T>
{
    public override bool IsSuccess => false;

    public ErrorDetails Error { get; } = error ?? throw new ArgumentNullException(nameof(error), "Error cannot be null in a failure result.");
}
