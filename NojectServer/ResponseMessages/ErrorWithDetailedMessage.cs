namespace NojectServer.ResponseMessages;

public class ErrorWithDetailedMessage
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}