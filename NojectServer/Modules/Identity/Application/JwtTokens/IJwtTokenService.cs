using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.JwtTokens;

/// <summary>
/// Service for creating and validating JWT tokens used in authentication and two-factor authentication flows.
/// Handles generation of short-lived access tokens and TFA tokens, as well as validation of TFA tokens.
/// </summary>
/// TODO: Longer term, split this service into AccessTokenService and TfaTokenService for better separation of concerns.
public interface IJwtTokenService
{
    /// <summary>
    /// Creates a JWT access token for the specified user email.
    /// Access tokens are short-lived tokens used for API authentication.
    /// </summary>
    /// <param name="userId">The ID of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    string CreateAccessToken(Guid userId);

    /// <summary>
    /// Creates a JWT token for two-factor authentication (TFA) purposes.
    /// TFA tokens are short-lived tokens used during the two-factor authentication flow.
    /// </summary>
    /// <param name="userId">The ID of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    string CreateTfaToken(Guid userId);

    /// <summary>
    /// Validates a JWT token for two-factor authentication and extracts its payload.
    /// Verifies token authenticity and extracts user claims.
    /// </summary>
    /// <param name="token">The JWT token string to validate</param>
    /// <returns>Result containing TfaTokenClaims on success, or error details on failure</returns>
    Result<TfaTokenClaims> ValidateTfaToken(string token);
}
