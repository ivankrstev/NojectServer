namespace NojectServer.Modules.Identity.Api.Contracts.PendingEmailVerification;

public sealed class ChangePendingEmailRequest
{
    public required string NewEmail { get; init; }
}
