namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Represents a login result that requires two-factor authentication.
/// </summary>
/// <param name="TwoFactorToken">The token used for two-factor authentication.</param>
/// <param name="ExpiresAt">The expiration time of the two-factor token.</param>
public sealed record TwoFactorRequiredLoginResult(
    string TwoFactorToken,
    DateTimeOffset ExpiresAt)
    : LoginResult;
