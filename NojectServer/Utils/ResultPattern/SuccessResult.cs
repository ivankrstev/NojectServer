namespace NojectServer.Utils.ResultPattern;

public sealed class SuccessResult<T>(T value) : Result<T>
{
    public override bool IsSuccess => true;

    public T Value { get; } = value ?? throw new ArgumentNullException(nameof(value), "Value cannot be null in a success result.");
}
