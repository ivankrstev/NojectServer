namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Represents a successfully authenticated login result that contains authentication tokens.
/// </summary>
/// <param name="AccessToken">The access token issued upon successful authentication.</param>
/// <param name="RefreshToken">The refresh token issued upon successful authentication.</param>
/// <param name="AccessTokenExpiresAt">The expiration time of the access token.</param>
/// <param name="RefreshTokenExpiresAt">The expiration time of the refresh token.</param>
public sealed record AuthenticatedLoginResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt)
    : LoginResult;
