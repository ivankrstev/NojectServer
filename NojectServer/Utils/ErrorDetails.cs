namespace NojectServer.Utils;

public class ErrorDetails(string error, string message, int statusCode)
{
    public string Error { get; } = error;
    public string Message { get; } = message;
    public int StatusCode { get; } = statusCode;
}
