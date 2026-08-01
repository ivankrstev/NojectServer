using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;

/// <summary>
/// Coordinates TOTP enrollment and authentication with protected secret storage.
/// </summary>
internal sealed class TwoFactorAuthService(
    IUserRepository userRepository,
    ITotpService totpService,
    ITwoFactorSecretProtector secretProtector,
    ILogger<TwoFactorAuthService> logger) : ITwoFactorAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
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

        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);

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
                await _userRepository.SaveChangesAsync(cancellationToken);
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
    public async Task<Result> EnableAsync(
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
            return Result.Failure(failure.Error);
        }

        User user = ((SuccessResult<User>)userResult).Value;

        if (user.TwoFactorEnabled)
        {
            return Result.Failure(TwoFactorAuthErrors.AlreadyEnabled);
        }

        return await ValidateAndApplyAsync(
            user,
            code,
            static configuredUser => configuredUser.EnableTwoFactor(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DisableAsync(
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
            return Result.Failure(failure.Error);
        }

        User user = ((SuccessResult<User>)userResult).Value;

        return await ValidateAndApplyAsync(
            user,
            code,
            static configuredUser => configuredUser.DisableTwoFactor(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> ValidateCodeAsync(
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
            return Result.Failure(failure.Error);
        }

        User user = ((SuccessResult<User>)userResult).Value;

        return await ValidateAndApplyAsync(
            user,
            code,
            static _ => { },
            cancellationToken);
    }

    /// <summary>
    /// Verifies the code against the user's TOTP secret and, if valid, applies
    /// <paramref name="applyChange"/> and persists both changes in a single save.
    /// </summary>
    private async Task<Result> ValidateAndApplyAsync(
        User user,
        string? code,
        Action<User> applyChange,
        CancellationToken cancellationToken)
    {
        byte[] secret = _secretProtector.Unprotect(
            user.Id,
            user.ProtectedTwoFactorSecret!);

        bool codeConsumed;
        try
        {
            codeConsumed = TryConsumeCode(user, secret, code);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }

        if (!codeConsumed)
        {
            return Result.Failure(TwoFactorAuthErrors.InvalidCode);
        }

        applyChange(user);

        if (!await TrySaveConsumedCodeAsync(user, cancellationToken))
        {
            return Result.Failure(TwoFactorAuthErrors.InvalidCode);
        }

        return Result.Success();
    }

    /// <summary>
    /// Loads the user by id and confirms two-factor authentication is configured,
    /// optionally requiring that it also be enabled.
    /// </summary>
    private async Task<Result<User>> GetConfiguredUserAsync(
        Guid userId,
        bool requireEnabled,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<User>(TwoFactorAuthErrors.InvalidUserId);
        }

        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);

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

    /// <summary>
    /// Validates the code against the secret and, if correct, records the matched time step
    /// on the user to guard against replay. Does not persist the change.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the code is valid and its time step has not already been
    /// consumed; otherwise <see langword="false"/>.
    /// </returns>
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

    /// <summary>
    /// Saves pending changes on the user.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the save succeeded; <see langword="false"/> if a concurrent
    /// update was detected.
    /// </returns>
    private async Task<bool> TrySaveConsumedCodeAsync(
        User user,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userRepository.SaveChangesAsync(cancellationToken);
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
