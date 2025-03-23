using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Services.Auth.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Auth.Implementations;

/// <summary>
/// Implementation of the IRefreshTokenService interface that manages refresh tokens
/// for the application's authentication system.
///
/// This service handles the creation, validation, and revocation of refresh tokens,
/// persisting them in the database and enforcing their expiration policies.
/// It works together with the TokenService to generate the actual JWT refresh tokens.
/// </summary>
public class RefreshTokenService(DataContext dataContext, ITokenService tokenService) : IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for a user and stores it in the database.
    /// The token is created with a 14-day expiration period from the current UTC time.
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>The generated refresh token string</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    public async Task<string> GenerateRefreshTokenAsync(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        var refreshToken = new RefreshToken
        {
            Email = email,
            Token = tokenService.CreateRefreshToken(email),
            ExpireDate = DateTime.UtcNow.AddDays(14)
        };
        dataContext.RefreshTokens.Add(refreshToken);
        await dataContext.SaveChangesAsync();
        return refreshToken.Token;
    }

    /// <summary>
    /// Validates a refresh token by checking its existence in the database and ensuring
    /// it has not expired. Returns the RefreshToken entity if valid.
    /// </summary>
    /// <param name="token">The refresh token string to validate</param>
    /// <returns>The valid RefreshToken entity from the database</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    /// <exception cref="SecurityTokenException">Thrown when the token is not found or has expired</exception>
    public async Task<RefreshToken> ValidateRefreshTokenAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var refreshToken = await dataContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken == null || refreshToken.ExpireDate < DateTime.UtcNow)
            throw new SecurityTokenException("Invalid or expired refresh token.");
        return refreshToken;
    }

    /// <summary>
    /// Revokes a refresh token by removing it from the database.
    /// This method is typically called during user logout or when refresh tokens need
    /// to be invalidated for security reasons.
    /// </summary>
    /// <param name="token">The refresh token string to revoke</param>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    /// <remarks>
    /// If the token doesn't exist in the database, this method completes without throwing an exception.
    /// </remarks>
    public async Task RevokeRefreshTokenAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var refreshToken = await dataContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            dataContext.RefreshTokens.Remove(refreshToken);
            await dataContext.SaveChangesAsync();
        }
    }
}
