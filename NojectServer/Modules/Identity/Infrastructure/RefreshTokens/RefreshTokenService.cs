using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;
using NojectServer.Data;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Infrastructure.RefreshTokens;

public class RefreshTokenService(
    DataContext dbContext,
    IOptions<RefreshTokenOptions> options,
    IRefreshTokenGenerator refreshTokenGenerator,
    TimeProvider timeProvider,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    private readonly DataContext _dbContext = dbContext;
    private readonly RefreshTokenOptions _options = options.Value;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = refreshTokenGenerator;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<RefreshTokenService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IssuedRefreshToken>> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<IssuedRefreshToken>(
                RefreshTokenErrors.InvalidUserId);
        }

        GeneratedRefreshToken generatedToken = _refreshTokenGenerator.Generate();

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        var refreshToken = RefreshToken.CreateNewSession(
            userId: userId,
            tokenHash: generatedToken.Hash,
            createdAt: utcNow,
            expiresAt: utcNow.AddDays(
                _options.ExpirationInDays));

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new IssuedRefreshToken(
                Token: generatedToken.PlainTextToken,
                ExpiresAt: refreshToken.ExpiresAt));
    }

    /// <inheritdoc />
    public async Task<Result<RotatedRefreshToken>> RotateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.InvalidToken);
        }

        byte[] tokenHash = _refreshTokenGenerator.ComputeHash(token);

        RefreshToken? currentToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(
                rt => rt.TokenHash == tokenHash,
                cancellationToken);

        if (currentToken is null)
        {
            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.InvalidToken);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        // A token with ReplacedByTokenId was already exchanged.
        // Receiving it again indicates a possible replay attack.
        if (currentToken.WasRotated)
        {
            await RevokeFamilyAsync(
                familyId: currentToken.FamilyId,
                revokedAt: utcNow,
                cancellationToken);

            _logger.LogWarning(
                $"Refresh token reuse detected for user {currentToken.UserId} " +
                $"and family {currentToken.FamilyId}.");
            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.ReuseDetected);
        }

        if (currentToken.IsRevoked)
        {
            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.RevokedToken);
        }

        if (currentToken.IsExpired(utcNow))
        {
            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.ExpiredToken);
        }

        try
        {
            // Generate a new token for the same login session.
            GeneratedRefreshToken generatedToken = _refreshTokenGenerator.Generate();

            // Create a replacement token in the same login-session family.
            RefreshToken replacementToken = currentToken.CreateReplacement(
                newTokenHash: generatedToken.Hash,
                createdAt: utcNow);

            // Mark the current token as rotated and link it to the replacement.
            currentToken.MarkAsRotated(replacementToken.Id, utcNow);

            // Add the replacement token to the database.
            _dbContext.RefreshTokens.Add(replacementToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new RotatedRefreshToken(
                    UserId: currentToken.UserId,
                    Token: generatedToken.PlainTextToken,
                    ExpiresAt: replacementToken.ExpiresAt));
        }
        // Handle concurrency exceptions that may occur
        // if multiple requests attempt to rotate the same token simultaneously.
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                $"Concurrent refresh token exchange detected for user {currentToken.UserId} " +
                $"and family {currentToken.FamilyId}.");

            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.ConcurrentExchange);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "Refresh token rotation failed because" +
                $" the token was no longer active for family {currentToken.FamilyId}.");

            return Result.Failure<RotatedRefreshToken>(
                RefreshTokenErrors.ConcurrentExchange);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RevokeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<bool>(
                RefreshTokenErrors.InvalidToken);
        }

        byte[] tokenHash = _refreshTokenGenerator.ComputeHash(token);

        RefreshToken? refreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                rt => rt.TokenHash == tokenHash,
                cancellationToken);

        // Make logout idempotent and avoid revealing whether
        // the supplied token existed.
        if (refreshToken is null)
        {
            return Result.Success(true);
        }

        await RevokeFamilyAsync(
            familyId: refreshToken.FamilyId,
            revokedAt: _timeProvider.GetUtcNow(),
            cancellationToken);

        return Result.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(
                RefreshTokenErrors.InvalidUserId);
        }

        DateTimeOffset revokedAt = _timeProvider.GetUtcNow();

        await _dbContext.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        token => token.RevokedAt,
                        revokedAt)
                    .SetProperty(
                        token => token.ConcurrencyToken,
                        Guid.NewGuid()),
                cancellationToken);

        return Result.Success();
    }

    // Revoke all tokens in the same family that are not already revoked,
    // preventing further use of any tokens in that login session.
    private Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        // Use ExecuteUpdateAsync for efficient bulk update without loading entities into memory.
        return _dbContext.RefreshTokens
            .Where(rt => rt.FamilyId == familyId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        rt => rt.RevokedAt,
                        revokedAt)
                    .SetProperty(
                        rt => rt.ConcurrencyToken,
                        Guid.NewGuid()),
                cancellationToken);
    }
}
