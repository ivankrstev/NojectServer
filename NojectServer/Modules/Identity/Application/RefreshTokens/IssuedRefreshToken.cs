namespace NojectServer.Modules.Identity.Application.RefreshTokens;

public sealed record IssuedRefreshToken(
    string Token,
    DateTimeOffset ExpiresAt);
