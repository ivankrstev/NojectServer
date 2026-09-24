namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Contains the information required to verify an email address.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Token">The opaque token received through the verification link.</param>
public sealed record VerifyEmailInput(
    string? Email,
    string? Token);
