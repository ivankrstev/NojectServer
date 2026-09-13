using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;
using NojectServer.Utils.Validation;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Authenticates users with their credentials, coordinates two-factor
/// authentication challenges, and issues access and refresh tokens.
/// </summary>
internal sealed class LoginService(
    IValidator<LoginInput> loginInputValidator,
    IValidator<CompleteTwoFactorLoginInput> completeTwoFactorLoginInputValidator,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    ITwoFactorAuthService twoFactorAuthService,
    ILogger<LoginService> logger) : ILoginService
{
    private readonly IValidator<LoginInput> _loginInputValidator = loginInputValidator;
    private readonly IValidator<CompleteTwoFactorLoginInput> _completeTwoFactorLoginInputValidator =
        completeTwoFactorLoginInputValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly ITwoFactorAuthService _twoFactorAuthService = twoFactorAuthService;
    private readonly ILogger<LoginService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<LoginResult>> LoginAsync(
        LoginInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _loginInputValidator.ValidateAsync(
                input,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure<LoginResult>(
                validationResult.ToErrorDictionary());
        }

        User? user = await _userRepository.GetByEmailAsync(
            input.Email!,
            cancellationToken);

        if (user is null)
        {
            PerformDummyPasswordHash(input.Password!);

            return Result.Failure<LoginResult>(
                LoginErrors.InvalidCredentials);
        }

        if (!_passwordHasher.Verify(
                input.Password!,
                user.PasswordHash,
                user.PasswordSalt))
        {
            return Result.Failure<LoginResult>(
                LoginErrors.InvalidCredentials);
        }

        if (user.VerifiedAt is null)
        {
            // Valid credentials establish only a restricted pending-verification
            // session. This token is accepted exclusively by pending-verification endpoints.
            GeneratedJwtToken pendingVerificationToken =
                _jwtTokenService.CreatePendingEmailVerificationToken(user.Id);

            return Result.Success<LoginResult>(
                new EmailVerificationRequiredLoginResult(
                    PendingVerificationToken: pendingVerificationToken.Token,
                    ExpiresAt: pendingVerificationToken.ExpiresAt,
                    MaskedEmail: MaskEmail(user.Email)));
        }

        if (user.TwoFactorEnabled)
        {
            GeneratedJwtToken twoFactorToken = _jwtTokenService.CreateTfaToken(user.Id);

            return Result.Success<LoginResult>(
                new TwoFactorRequiredLoginResult(
                    TwoFactorToken: twoFactorToken.Token,
                    ExpiresAt: twoFactorToken.ExpiresAt));
        }

        Result<AuthenticatedLoginResult> authenticatedResult =
            await IssueAuthenticatedLoginAsync(user.Id, cancellationToken);

        return authenticatedResult switch
        {
            SuccessResult<AuthenticatedLoginResult> success =>
                Result.Success<LoginResult>(success.Value),

            FailureResult<AuthenticatedLoginResult> failure =>
                Result.Failure<LoginResult>(failure.Error),

            _ => throw new InvalidOperationException(
                $"Unsupported result type: {authenticatedResult.GetType().Name}.")
        };
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedLoginResult>> CompleteTwoFactorLoginAsync(
        CompleteTwoFactorLoginInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _completeTwoFactorLoginInputValidator.ValidateAsync(
                input,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure<AuthenticatedLoginResult>(
                validationResult.ToErrorDictionary());
        }

        Result<TfaTokenClaims> tokenResult =
            _jwtTokenService.ValidateTfaToken(input.TfaToken!);

        if (tokenResult is not
            SuccessResult<TfaTokenClaims> tokenSuccess)
        {
            return Result.Failure<AuthenticatedLoginResult>(
                LoginErrors.InvalidOrExpiredTwoFactorChallenge);
        }

        TfaTokenClaims tokenClaims = tokenSuccess.Value;

        Result codeValidationResult =
            await _twoFactorAuthService.ValidateCodeAsync(
                tokenClaims.UserId,
                input.Code!,
                cancellationToken);

        if (codeValidationResult.IsFailure)
        {
            return Result.Failure<AuthenticatedLoginResult>(
                LoginErrors.TwoFactorAuthenticationFailed);
        }

        // The challenge represents a previously valid password login. Recheck
        // mutable account eligibility before creating a full session.
        User? currentUser = await _userRepository.GetByIdAsync(
            tokenClaims.UserId,
            cancellationToken);

        if (currentUser?.VerifiedAt is null || !currentUser.TwoFactorEnabled)
        {
            return Result.Failure<AuthenticatedLoginResult>(
                LoginErrors.InvalidOrExpiredTwoFactorChallenge);
        }

        return await IssueAuthenticatedLoginAsync(tokenClaims.UserId, cancellationToken);
    }

    private static string MaskEmail(string email)
    {
        int atIndex = email.IndexOf('@');

        if (atIndex <= 0)
        {
            return "***";
        }

        string localPart = email[..atIndex];
        string domain = email[atIndex..];

        string maskedLocalPart = localPart.Length == 1
            ? $"{localPart[0]}***"
            : $"{localPart[0]}***{localPart[^1]}";

        return maskedLocalPart + domain;
    }

    /// <summary>
    /// Performs a password-hashing operation for an unknown user to reduce
    /// observable timing differences between failed login attempts.
    /// </summary>
    private void PerformDummyPasswordHash(string password)
    {
        HashedPassword dummyCredentials = _passwordHasher.Hash(password);

        CryptographicOperations.ZeroMemory(dummyCredentials.Hash);
        CryptographicOperations.ZeroMemory(dummyCredentials.Salt);
    }

    /// <summary>
    /// Issues the access and refresh tokens required for an authenticated login.
    /// </summary>
    /// <returns>
    /// A successful result containing the issued access token, refresh token,
    /// and their expiration times, or a failed result when refresh-token
    /// issuance fails.
    /// </returns>
    private async Task<Result<AuthenticatedLoginResult>> IssueAuthenticatedLoginAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        GeneratedJwtToken accessToken =
            _jwtTokenService.CreateAccessToken(userId);

        Result<IssuedRefreshToken> refreshTokenResult =
            await _refreshTokenService.IssueAsync(userId, cancellationToken);

        if (refreshTokenResult is not
            SuccessResult<IssuedRefreshToken> refreshTokenSuccess)
        {
            _logger.LogError(
                "Refresh-token issuance failed for user {UserId} with error {Error}.",
                userId,
                refreshTokenResult.Error?.Error ?? "Unknown");

            return Result.Failure<AuthenticatedLoginResult>(
                LoginErrors.AuthenticationCompletionFailed);
        }

        IssuedRefreshToken issuedRefreshToken = refreshTokenSuccess.Value;

        return Result.Success(
            new AuthenticatedLoginResult(
                AccessToken: accessToken.Token,
                RefreshToken: issuedRefreshToken.Token,
                AccessTokenExpiresAt: accessToken.ExpiresAt,
                RefreshTokenExpiresAt: issuedRefreshToken.ExpiresAt));
    }
}
