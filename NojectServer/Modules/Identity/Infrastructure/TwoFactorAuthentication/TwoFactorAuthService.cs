using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Modules.Identity.Application.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

/// <summary>
/// Coordinates TOTP enrollment and authentication with protected secret storage.
/// </summary>
internal sealed class TwoFactorAuthService(
    DataContext dbContext,
    ITotpService totpService,
    ITwoFactorSecretProtector secretProtector,
    ILogger<TwoFactorAuthService> logger) : ITwoFactorAuthService
{
    private readonly DataContext _dbContext = dbContext;
    private readonly ITotpService _totpService = totpService;
    private readonly ITwoFactorSecretProtector _secretProtector = secretProtector;
    private readonly ILogger<TwoFactorAuthService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<TwoFactorSetup>> GenerateSetupCodeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<TwoFactorSetup>(TwoFactorAuthErrors.InvalidUserId);
        }

        User? user = await FindUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<TwoFactorSetup>(TwoFactorAuthErrors.UserNotFound);
        }

        if (user.TwoFactorEnabled)
        {
            return Result.Failure<TwoFactorSetup>(TwoFactorAuthErrors.AlreadyEnabled);
        }

        byte[] secret = _totpService.GenerateSecret();

        try
        {
            byte[] protectedSecret = _secretProtector.Protect(user.Id, secret);
            user.SetProtectedTwoFactorSecret(protectedSecret);

            string manualKey = _totpService.EncodeSecret(secret);
            string provisioningUri = _totpService.CreateProvisioningUri(secret, user.Email);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Concurrent two-factor setup detected for user {UserId}.",
                    user.Id);

                return Result.Failure<TwoFactorSetup>(
                    TwoFactorAuthErrors.ConcurrentSetup);
            }

            return Result.Success(
                new TwoFactorSetup(
                    ManualKey: manualKey,
                    ProvisioningUri: provisioningUri
            ));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> EnableAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        Result<User> userResult = await GetConfiguredUserAsync(
            userId,
            requireEnabled: false,
            cancellationToken);

        if (userResult is FailureResult<User> failure)
        {
            return Result.Failure<string>(failure.Error);
        }

        User user = ((SuccessResult<User>)userResult).Value;

        if (user.TwoFactorEnabled)
        {
            return Result.Failure<string>(TwoFactorAuthErrors.AlreadyEnabled);
        }

        return await ValidateAndApplyAsync(
            user,
            code,
            static configuredUser => configuredUser.EnableTwoFactor(),
            "Two-factor authentication enabled successfully.",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<string>> DisableAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        Result<User> userResult = await GetConfiguredUserAsync(
            userId,
            requireEnabled: true,
            cancellationToken);

        if (userResult is FailureResult<User> failure)
        {
            return Result.Failure<string>(failure.Error);
        }

        User user = ((SuccessResult<User>)userResult).Value;

        return await ValidateAndApplyAsync(
            user,
            code,
            static configuredUser => configuredUser.DisableTwoFactor(),
            "Two-factor authentication disabled successfully.",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ValidateCodeAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        Result<User> userResult = await GetConfiguredUserAsync(
            userId,
            requireEnabled: true,
            cancellationToken);

        if (userResult is FailureResult<User> failure)
        {
            return Result.Failure<bool>(failure.Error);
        }

        User user = ((SuccessResult<User>)userResult).Value;
        byte[] secret = _secretProtector.Unprotect(
            user.Id,
            user.ProtectedTwoFactorSecret!);

        try
        {
            if (!TryConsumeCode(user, secret, code)
                || !await TrySaveConsumedCodeAsync(user, cancellationToken))
            {
                return Result.Failure<bool>(TwoFactorAuthErrors.InvalidCode);
            }

            return Result.Success(true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    private async Task<Result<string>> ValidateAndApplyAsync(
        User user,
        string? code,
        Action<User> applyChange,
        string successMessage,
        CancellationToken cancellationToken)
    {
        byte[] secret = _secretProtector.Unprotect(
            user.Id,
            user.ProtectedTwoFactorSecret!);

        try
        {
            if (!TryConsumeCode(user, secret, code))
            {
                return Result.Failure<string>(TwoFactorAuthErrors.InvalidCode);
            }

            applyChange(user);

            if (!await TrySaveConsumedCodeAsync(user, cancellationToken))
            {
                return Result.Failure<string>(TwoFactorAuthErrors.InvalidCode);
            }

            return Result.Success(successMessage);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    private async Task<Result<User>> GetConfiguredUserAsync(
        Guid userId,
        bool requireEnabled,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<User>(TwoFactorAuthErrors.InvalidUserId);
        }

        User? user = await FindUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<User>(TwoFactorAuthErrors.UserNotFound);
        }

        if (requireEnabled && !user.TwoFactorEnabled)
        {
            return Result.Failure<User>(TwoFactorAuthErrors.NotEnabled);
        }

        if (user.ProtectedTwoFactorSecret is null)
        {
            return Result.Failure<User>(TwoFactorAuthErrors.NotConfigured);
        }

        return Result.Success(user);
    }

    private Task<User?> FindUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken);
    }

    private bool TryConsumeCode(
        User user,
        byte[] secret,
        string? code)
    {
        if (!_totpService.TryValidateCode(
                secret,
                code,
                out long matchedTimeStep))
        {
            return false;
        }

        try
        {
            user.RecordAcceptedTotpTimeStep(matchedTimeStep);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private async Task<bool> TrySaveConsumedCodeAsync(
        User user,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Concurrent TOTP code use detected for user {UserId}.",
                user.Id);

            return false;
        }
    }
}
