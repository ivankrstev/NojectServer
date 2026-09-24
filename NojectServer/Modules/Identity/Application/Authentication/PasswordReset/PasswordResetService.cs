using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Persistence;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.Tokens;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;
using NojectServer.Utils.Validation;

namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Implements the password-reset workflow using hashed, expiring opaque tokens.
/// </summary>
internal sealed class PasswordResetService(
    IValidator<RequestPasswordResetInput> requestValidator,
    IValidator<ResetPasswordInput> resetValidator,
    IUserRepository userRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    IRefreshTokenService refreshTokenService,
    IIdentityTransaction identityTransaction,
    TimeProvider timeProvider,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private readonly IValidator<RequestPasswordResetInput> _requestValidator = requestValidator;
    private readonly IValidator<ResetPasswordInput> _resetValidator = resetValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOpaqueTokenGenerator _tokenGenerator = tokenGenerator;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IEmailService _emailService = emailService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly IIdentityTransaction _identityTransaction = identityTransaction;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<PasswordResetService> _logger = logger;

    private const int ResetTokenSizeInBytes = 32;

    private static readonly TimeSpan s_resetTokenLifetime =
        TimeSpan.FromHours(1);

    /// <inheritdoc />
    public async Task<Result> RequestResetAsync(
        RequestPasswordResetInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _requestValidator.ValidateAsync(input, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure(
                validationResult.ToErrorDictionary());
        }

        User? user = await _userRepository.GetByEmailAsync(
            input.Email!,
            cancellationToken);

        // Always return the same response for a valid request so this operation
        // cannot be used to discover registered email addresses.
        if (user is null)
        {
            return Result.Success();
        }

        GeneratedToken generatedToken = _tokenGenerator.Generate(ResetTokenSizeInBytes);
        DateTimeOffset issuedAt = _timeProvider.GetUtcNow();

        user.SetPasswordResetToken(
            generatedToken.Hash,
            issuedAt,
            issuedAt.Add(s_resetTokenLifetime));

        await _userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailService.SendResetPasswordLinkAsync(
                user.Email,
                user.FullName,
                generatedToken.PlainText,
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            // Returning a different response here would reveal that the supplied
            // email belongs to an account. In production, an outbox should be
            // used so failed deliveries can be retried reliably.
            _logger.LogError(
                exception,
                "Failed to deliver a password-reset email for user {UserId}.",
                user.Id);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(
        ResetPasswordInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _resetValidator.ValidateAsync(input, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure(
                validationResult.ToErrorDictionary());
        }

        byte[] suppliedHash =
            _tokenGenerator.ComputeHash(input.ResetToken!);

        User? user = await _userRepository.GetByPasswordResetTokenHashAsync(
            suppliedHash,
            cancellationToken);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (!HasValidResetToken(user, input.ResetToken!, utcNow))
        {
            return Result.Failure(
                PasswordResetErrors.InvalidOrExpiredToken);
        }

        HashedPassword credentials = _passwordHasher.Hash(input.NewPassword!);

        try
        {
            user!.ChangePassword(credentials.Hash, credentials.Salt);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(credentials.Hash);
            CryptographicOperations.ZeroMemory(credentials.Salt);
        }

        return await _identityTransaction.ExecuteAsync(
            async transactionCancellationToken =>
            {
                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);

                Result revocationResult =
                    await _refreshTokenService.RevokeAllForUserAsync(
                        user.Id,
                        transactionCancellationToken);

                if (revocationResult is FailureResult failure)
                {
                    return Result.Failure(failure.Error);
                }

                return Result.Success();
            },
            cancellationToken);
    }

    /// <summary>
    /// Validates the supplied token hash and expiration without timing-sensitive comparison.
    /// </summary>
    private bool HasValidResetToken(
        User? user,
        string resetToken,
        DateTimeOffset utcNow)
    {
        if (user?.PasswordResetTokenHash is not ReadOnlyMemory<byte> persistedHash ||
            user.PasswordResetTokenExpiresAt is not DateTimeOffset expiresAt ||
            expiresAt <= utcNow)
        {
            return false;
        }

        byte[] suppliedHash = _tokenGenerator.ComputeHash(resetToken);

        return suppliedHash.Length == persistedHash.Length &&
               CryptographicOperations.FixedTimeEquals(
                   suppliedHash,
                   persistedHash.Span);
    }
}
