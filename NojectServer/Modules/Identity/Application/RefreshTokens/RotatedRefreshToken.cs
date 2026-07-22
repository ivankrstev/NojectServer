namespace NojectServer.Modules.Identity.Application.RefreshTokens;

public sealed record RotatedRefreshToken(
    Guid UserId,
    string Token,
    DateTimeOffset ExpiresAt);
