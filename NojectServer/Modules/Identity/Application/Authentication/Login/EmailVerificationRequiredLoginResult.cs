namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Represents a valid password login that may access only the pending
/// email-verification workflow.
/// </summary>
/// <param name="PendingVerificationToken">
/// The restricted token accepted only by pending-verification endpoints.
/// </param>
/// <param name="ExpiresAt">The expiration time of the restricted token.</param>
/// <param name="MaskedEmail">The account email in a display-safe masked form.</param>
public sealed record EmailVerificationRequiredLoginResult(
    string PendingVerificationToken,
    DateTimeOffset ExpiresAt,
    string MaskedEmail)
    : LoginResult;
