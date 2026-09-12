namespace NojectServer.Modules.Identity.Application.JwtTokens;

/// <summary>
/// Contains a generated JWT token and its expiration time.
/// </summary>
/// <param name="Token">The serialized JWT token string.</param>
/// <param name="ExpiresAt">The expiration time of the token.</param>
public sealed record GeneratedJwtToken(
    string Token,
    DateTimeOffset ExpiresAt);
