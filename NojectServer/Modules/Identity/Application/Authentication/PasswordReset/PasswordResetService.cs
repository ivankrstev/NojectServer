using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Application.Passwords;
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
    TimeProvider timeProvider,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private readonly IValidator<RequestPasswordResetInput> _requestValidator = requestValidator;
    private readonly IValidator<ResetPasswordInput> _resetValidator = resetValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOpaqueTokenGenerator _tokenGenerator = tokenGenerator;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IEmailService _emailService = emailService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<PasswordResetService> _logger = logger;

    private const int ResetTokenSizeInBytes = 32;

    private static readonly TimeSpan s_resetTokenLifetime =
        TimeSpan.FromHours(1);

    private static readonly ErrorDetails s_invalidOrExpiredToken = new(
        "PasswordReset.InvalidOrExpiredToken",
        "The password reset token is invalid or has expired.",
        StatusCodes.Status400BadRequest);

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

        User? user = await _userRepository.GetByEmailAsync(
            input.Email!,
            cancellationToken);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (!HasValidResetToken(user, input.ResetToken!, utcNow))
        {
            return Result.Failure(s_invalidOrExpiredToken);
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

        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Validates the supplied token hash and expiration without timing-sensitive comparison.
    /// </summary>
    private bool HasValidResetToken(
        User? user,
        string resetToken,
        DateTimeOffset utcNow)
    {
        if (user?.PasswordResetTokenHash is not byte[] persistedHash ||
            user.PasswordResetTokenExpiresAt is not DateTimeOffset expiresAt ||
            expiresAt <= utcNow)
        {
            return false;
        }

        byte[] suppliedHash = _tokenGenerator.ComputeHash(resetToken);

        return suppliedHash.Length == persistedHash.Length &&
               CryptographicOperations.FixedTimeEquals(
                   suppliedHash,
                   persistedHash);
    }
}
