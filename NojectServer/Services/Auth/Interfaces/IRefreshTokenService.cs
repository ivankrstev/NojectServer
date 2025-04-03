using NojectServer.Models;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Interfaces;

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
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A Task representing the asynchronous operation, containing the generated refresh token string</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    Task<Result<string>> GenerateRefreshTokenAsync(string email);

    /// <summary>
    /// Validates a refresh token by checking its existence and expiration date.
    /// </summary>
    /// <param name="token">The refresh token string to validate</param>
    /// <returns>A Task representing the asynchronous operation, containing the valid RefreshToken entity</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    /// <exception cref="SecurityTokenException">Thrown when the token is invalid or expired</exception>
    Task<Result<RefreshToken>> ValidateRefreshTokenAsync(string token);

    /// <summary>
    /// Revokes a refresh token by removing it from the database.
    /// This should be called during logout or when refresh tokens need to be invalidated.
    /// </summary>
    /// <param name="token">The refresh token string to revoke</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    Task<Result<bool>> RevokeRefreshTokenAsync(string token);
}
