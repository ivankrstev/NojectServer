namespace NojectServer.Modules.Identity.Api.Contracts.PendingEmailVerification;

public sealed class DeletePendingAccountRequest
{
    public required string Password { get; init; }
}
