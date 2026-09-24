namespace NojectServer.Modules.Identity.Api.Contracts.Authentication;

/// <summary>
/// Completes a password login that requires an authenticator code.
/// </summary>
public sealed class CompleteTwoFactorLoginRequest
{
    public required string TfaToken { get; init; }

    public required string Code { get; init; }
}
