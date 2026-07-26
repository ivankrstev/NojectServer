namespace NojectServer.Modules.Identity.Application.Authentication.Login;

public sealed record TwoFactorRequiredLoginResult(
    string TwoFactorToken,
    DateTimeOffset ExpiresAt)
    : LoginResult;
