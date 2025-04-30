using NojectServer.Models;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Implementations;

/// <summary>
/// Implementation of the IRefreshTokenService interface that manages refresh tokens
/// for the application's authentication system.
///
/// This service handles the creation, validation, and revocation of refresh tokens,
/// persisting them in the database and enforcing their expiration policies.
/// It works together with the TokenService to generate the actual JWT refresh tokens.
/// </summary>
public class RefreshTokenService(IUnitOfWork unitOfWork, ITokenService tokenService) : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository = unitOfWork.GetRepository<RefreshToken>() as IRefreshTokenRepository
        ?? throw new InvalidOperationException("Failed to get refresh token repository");

    /// <summary>
    /// Generates a new refresh token for a user and stores it in the database.
    /// The token is created with a 14-day expiration period from the current UTC time.
    /// </summary>
    /// <param name="userId">The ID of the user to create the token for</param>
    /// <param name="email">The email address of the user used for claims</param>
    /// <returns>A Result containing the generated refresh token string</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    public async Task<Result<string>> GenerateRefreshTokenAsync(Guid userId, string email)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(email);
        try
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = _tokenService.CreateRefreshToken(userId, email),
                ExpireDate = DateTime.UtcNow.AddDays(14)
            };
            await _refreshTokenRepository.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success(refreshToken.Token);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("TokenGenerationFailed", $"Failed to generate refresh token: {ex.Message}",
                500);
        }
    }

    /// <summary>
    /// Validates a refresh token by checking its existence in the database and ensuring
    /// it has not expired. Returns the RefreshToken entity if valid.
    /// </summary>
    /// <param name="token">The refresh token string to validate</param>
    /// <returns>A Result containing the valid RefreshToken entity or failure details</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    public async Task<Result<RefreshToken>> ValidateRefreshTokenAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null)
            return Result.Failure<RefreshToken>("InvalidToken", "Invalid refresh token.", 401);

        if (refreshToken.ExpireDate < DateTime.UtcNow)
            return Result.Failure<RefreshToken>("ExpiredToken", "Refresh token has expired.", 401);

        return Result.Success(refreshToken);
    }

    /// <summary>
    /// Revokes a refresh token by removing it from the database.
    /// This method is typically called during user logout or when refresh tokens need
    /// to be invalidated for security reasons.
    /// </summary>
    /// <param name="token">The refresh token string to revoke</param>
    /// <returns>A Result indicating success or failure of the operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    /// <remarks>
    /// If the token doesn't exist in the database, this method completes without throwing an exception.
    /// </remarks>
    public async Task<Result<bool>> RevokeRefreshTokenAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

            if (refreshToken != null)
            {
                _refreshTokenRepository.Remove(refreshToken);
                await _unitOfWork.SaveChangesAsync();
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>("RevocationFailed",
                $"Failed to revoke refresh token: {ex.Message}", 500);
        }
    }
}
