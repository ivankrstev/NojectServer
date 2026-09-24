namespace NojectServer.Modules.Identity.Api.Contracts.Authentication;

public sealed record AuthenticatedLoginResponse(
    string Status,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record TwoFactorRequiredResponse(
    string Status,
    string TwoFactorToken,
    DateTimeOffset ExpiresAt);

public sealed record EmailVerificationRequiredResponse(
    string Status,
    string PendingVerificationToken,
    DateTimeOffset ExpiresAt,
    string MaskedEmail);
