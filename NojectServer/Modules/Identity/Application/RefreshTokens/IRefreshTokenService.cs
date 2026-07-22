using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// Defines the contract for refresh token management operations.
/// This interface provides methods to generate, validate, and revoke refresh tokens
/// which are used in the authentication workflow to obtain new access tokens
/// without requiring users to re-authenticate.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for a user and stores it in the database.
    /// </summary>
    /// <param name="userId">The ID of the user to create the token for</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>
    /// A Result containing an IssuedRefreshToken with the plain-text refresh token and its expiration time on success, or error details on failure.
    /// </returns>
    Task<Result<IssuedRefreshToken>> IssueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and consumes a refresh token, replacing it with a newly issued token.
    /// </summary>
    /// <param name="token">The refresh token string to validate</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>
    /// A Result containing a RotatedRefreshToken with the user ID, the new plain-text refresh token and its expiration time on success, or error details on failure.
    /// </returns>
    Task<Result<RotatedRefreshToken>> RotateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the login session associated with the supplied refresh token.
    /// Revoked tokens are retained for rotation history and reuse detection.
    /// </summary>
    /// <param name="token">The refresh token string to revoke</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A Result indicating whether the revocation completed successfully.</returns>
    Task<Result<bool>> RevokeAsync(string token, CancellationToken cancellationToken = default);
}
