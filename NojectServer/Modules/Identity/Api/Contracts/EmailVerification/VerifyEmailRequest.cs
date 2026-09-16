namespace NojectServer.Modules.Identity.Api.Contracts.EmailVerification;

/// <summary>
/// Represents a request to verify a user's email address.
/// Contains the email address to verify and the associated verification token.
/// </summary>
public sealed class VerifyEmailRequest
{
    public required string Email { get; init; }

    public required string Token { get; init; }
}
