namespace NojectServer.Modules.Identity.Api.Contracts.EmailVerification;

public sealed class RequestEmailVerificationRequest
{
    public required string Email { get; init; }
}
