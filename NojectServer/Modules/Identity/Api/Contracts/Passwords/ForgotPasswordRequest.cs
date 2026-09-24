namespace NojectServer.Modules.Identity.Api.Contracts.Passwords;

public sealed class ForgotPasswordRequest
{
    public required string Email { get; init; }
}
