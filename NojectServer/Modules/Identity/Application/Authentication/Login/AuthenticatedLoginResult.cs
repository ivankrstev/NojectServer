namespace NojectServer.Modules.Identity.Application.Authentication.Login;

public sealed record AuthenticatedLoginResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt)
    : LoginResult;
