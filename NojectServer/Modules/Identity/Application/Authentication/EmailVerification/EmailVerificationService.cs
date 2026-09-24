using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Application.Tokens;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;
using NojectServer.Utils.Validation;

namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Issues and consumes hashed, expiring email-verification tokens.
/// </summary>
internal sealed class EmailVerificationService(
    IValidator<RequestEmailVerificationInput> requestEmailVerificationInputValidator,
    IValidator<VerifyEmailInput> verifyEmailInputValidator,
    IUserRepository userRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IEmailService emailService,
    TimeProvider timeProvider,
    ILogger<EmailVerificationService> logger) : IEmailVerificationService
{
    private readonly IValidator<RequestEmailVerificationInput> _requestValidator =
        requestEmailVerificationInputValidator;
    private readonly IValidator<VerifyEmailInput> _verifyValidator =
        verifyEmailInputValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOpaqueTokenGenerator _tokenGenerator = tokenGenerator;
    private readonly IEmailService _emailService = emailService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<EmailVerificationService> _logger = logger;

    private const int VerificationTokenSizeInBytes = 32;

    private static readonly TimeSpan s_verificationTokenLifetime =
        TimeSpan.FromHours(24);

    private static readonly ErrorDetails s_invalidOrExpiredToken = new(
        "EmailVerification.InvalidOrExpiredToken",
        "The email verification token is invalid or has expired.",
        StatusCodes.Status400BadRequest);

    /// <inheritdoc />
    public async Task<Result> RequestVerificationAsync(
        RequestEmailVerificationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _requestValidator.ValidateAsync(
                input,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure(
                validationResult.ToErrorDictionary());
        }

        User? user = await _userRepository.GetByEmailAsync(
            input.Email!,
            cancellationToken);

        // Do not disclose whether the account exists or is already verified.
        if (user is null || user.VerifiedAt is not null)
        {
            return Result.Success();
        }

        GeneratedToken generatedToken =
            _tokenGenerator.Generate(VerificationTokenSizeInBytes);

        DateTimeOffset issuedAt = _timeProvider.GetUtcNow();

        user.SetEmailVerificationToken(
            generatedToken.Hash,
            issuedAt,
            issuedAt.Add(s_verificationTokenLifetime));

        await _userRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailService.SendVerificationLinkAsync(
                user.Email,
                user.FullName,
                generatedToken.PlainText,
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            // A different response would disclose that the email belongs to an
            // unverified account. An outbox should eventually provide retries.
            _logger.LogError(
                exception,
                "Failed to deliver an email-verification message for user {UserId}.",
                user.Id);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyAsync(
        VerifyEmailInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _verifyValidator.ValidateAsync(
                input,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure(
                validationResult.ToErrorDictionary());
        }

        User? user = await _userRepository.GetByEmailAsync(
            input.Email!,
            cancellationToken);

        DateTimeOffset verifiedAt = _timeProvider.GetUtcNow();

        if (!HasValidVerificationToken(
                user,
                input.Token!,
                verifiedAt))
        {
            return Result.Failure(s_invalidOrExpiredToken);
        }

        user!.MarkAsVerified(verifiedAt);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Validates token expiration and compares token hashes in fixed time.
    /// </summary>
    private bool HasValidVerificationToken(
        User? user,
        string token,
        DateTimeOffset utcNow)
    {
        if (user?.VerifiedAt is not null ||
            user?.VerificationTokenHash is not ReadOnlyMemory<byte> persistedHash ||
            user.VerificationTokenExpiresAt is not DateTimeOffset expiresAt ||
            expiresAt <= utcNow)
        {
            return false;
        }

        byte[] suppliedHash = _tokenGenerator.ComputeHash(token);

        return suppliedHash.Length == persistedHash.Length &&
               CryptographicOperations.FixedTimeEquals(
                   suppliedHash,
                   persistedHash.Span);
    }
}
