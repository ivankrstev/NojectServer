using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// Contains predefined error details for refresh token operations, providing standardized error messages and status codes for various failure scenarios.
/// </summary>
/// <remarks>
/// This class is intended to be used throughout the application to ensure consistent error handling and messaging related to refresh token operations, such as issuing, rotating, and revoking tokens.
/// </remarks>
public static class RefreshTokenErrors
{
    public static readonly ErrorDetails InvalidUserId = new(
        "RefreshToken.InvalidUserId",
        "The user ID is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly ErrorDetails InvalidToken = new(
        "RefreshToken.Invalid",
        "The refresh token is invalid.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails ExpiredToken = new(
        "RefreshToken.Expired",
        "The refresh token has expired.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails RevokedToken = new(
        "RefreshToken.Revoked",
        "The refresh token has been revoked.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails ReuseDetected = new(
        "RefreshToken.ReuseDetected",
        "Refresh token reuse was detected.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails ConcurrentExchange = new(
        "RefreshToken.ConcurrentExchange",
        "The refresh token has already been exchanged.",
        StatusCodes.Status409Conflict);
}
