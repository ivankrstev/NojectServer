using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;
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
    IOptions<AccessTokenOptions> accessTokenOptions,
    IOptions<TfaTokenOptions> tfaTokenOptions,
    TimeProvider timeProvider) : ILoginService
{
    private readonly IValidator<LoginInput> _loginInputValidator = loginInputValidator;
    private readonly IValidator<CompleteTwoFactorLoginInput> _completeTwoFactorLoginInputValidator =
        completeTwoFactorLoginInputValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly ITwoFactorAuthService _twoFactorAuthService = twoFactorAuthService;
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;
    private readonly TfaTokenOptions _tfaTokenOptions = tfaTokenOptions.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

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
                AuthenticationErrors.InvalidCredentials);
        }

        if (!_passwordHasher.Verify(
                input.Password!,
                user.PasswordHash,
                user.PasswordSalt))
        {
            return Result.Failure<LoginResult>(
                AuthenticationErrors.InvalidCredentials);
        }

        if (user.VerifiedAt is null)
        {
            return Result.Failure<LoginResult>(
                AuthenticationErrors.EmailNotVerified);
        }

        DateTimeOffset issuedAt = _timeProvider.GetUtcNow();

        if (user.TwoFactorEnabled)
        {
            string twoFactorToken = _jwtTokenService.CreateTfaToken(user.Id);

            return Result.Success<LoginResult>(
                new TwoFactorRequiredLoginResult(
                    TwoFactorToken: twoFactorToken,
                    ExpiresAt: issuedAt.AddMinutes(_tfaTokenOptions.ExpirationInMinutes)));
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
            $"Unsupported result type: " +
            $"{authenticatedResult.GetType().Name}.")
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

        if (tokenResult is FailureResult<TfaTokenClaims> tokenFailure)
        {
            return Result.Failure<AuthenticatedLoginResult>(
                tokenFailure.Error);
        }

        TfaTokenClaims tokenClaims = ((SuccessResult<TfaTokenClaims>)tokenResult).Value;

        Result codeValidationResult =
            await _twoFactorAuthService.ValidateCodeAsync(
                tokenClaims.UserId,
                input.Code!,
                cancellationToken);

        if (codeValidationResult is FailureResult codeFailure)
        {
            return Result.Failure<AuthenticatedLoginResult>(
                codeFailure.Error);
        }

        return await IssueAuthenticatedLoginAsync(tokenClaims.UserId, cancellationToken);
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
        string accessToken = _jwtTokenService.CreateAccessToken(userId);

        Result<IssuedRefreshToken> refreshTokenResult =
            await _refreshTokenService.IssueAsync(userId, cancellationToken);

        if (refreshTokenResult is FailureResult<IssuedRefreshToken> refreshFailure)
        {
            return Result.Failure<AuthenticatedLoginResult>(
                refreshFailure.Error);
        }

        IssuedRefreshToken issuedRefreshToken =
            ((SuccessResult<IssuedRefreshToken>)refreshTokenResult).Value;

        DateTimeOffset issuedAt = _timeProvider.GetUtcNow();

        return Result.Success(
            new AuthenticatedLoginResult(
                AccessToken: accessToken,
                RefreshToken: issuedRefreshToken.Token,
                AccessTokenExpiresAt: issuedAt.AddMinutes(_accessTokenOptions.ExpirationInMinutes),
                RefreshTokenExpiresAt: issuedRefreshToken.ExpiresAt));
    }
}
