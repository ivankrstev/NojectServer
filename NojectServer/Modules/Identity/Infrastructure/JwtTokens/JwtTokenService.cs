using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Configurations.Tokens;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Infrastructure.JwtTokens;

/// <summary>
/// Implementation of JWT token service for creating and validating authentication tokens.
/// 
/// Generates short-lived access tokens and TFA tokens using HMAC-SHA256 signing.
/// Includes automatic token expiration based on configured durations and validates
/// tokens against issuer/audience claims.
/// </summary>
public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    IOptions<AccessTokenOptions> accessTokenOptions,
    IOptions<TfaTokenOptions> tfaTokenOptions,
    IOptions<PendingEmailVerificationTokenOptions> pendingEmailVerificationTokenOptions,
    JwtTokenValidationParametersFactory validationParametersFactory,
    TimeProvider timeProvider,
    ILogger<JwtTokenService> logger) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;
    private readonly TfaTokenOptions _tfaTokenOptions = tfaTokenOptions.Value;
    private readonly PendingEmailVerificationTokenOptions _pendingEmailVerificationTokenOptions =
        pendingEmailVerificationTokenOptions.Value;
    private readonly JwtTokenValidationParametersFactory _validationParametersFactory = validationParametersFactory;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<JwtTokenService> _logger = logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new()
    {
        MapInboundClaims = false
    };

    /// <inheritdoc/>
    public GeneratedJwtToken CreateAccessToken(Guid userId)
    {
        return CreateJwtToken(
            userId,
            _accessTokenOptions.SecretKey,
            _accessTokenOptions.ExpirationInMinutes,
            IdentityTokenConstants.AccessTokenPurpose);
    }

    /// <inheritdoc/>
    public GeneratedJwtToken CreateTfaToken(Guid userId)
    {
        return CreateJwtToken(
            userId,
            _tfaTokenOptions.SecretKey,
            _tfaTokenOptions.ExpirationInMinutes,
            IdentityTokenConstants.TfaTokenPurpose);
    }

    /// <inheritdoc/>
    public GeneratedJwtToken CreatePendingEmailVerificationToken(Guid userId)
    {
        return CreateJwtToken(
            userId,
            _pendingEmailVerificationTokenOptions.SecretKey,
            _pendingEmailVerificationTokenOptions.ExpirationInMinutes,
            IdentityTokenConstants.PendingEmailVerificationTokenPurpose);
    }

    /// <inheritdoc/>
    public Result<TfaTokenClaims> ValidateTfaToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Result.Failure<TfaTokenClaims>(
                "TfaToken.Required",
                "The TFA token is required.");
        }

        try
        {
            ClaimsPrincipal principal = _tokenHandler.ValidateToken(
                token,
                _validationParametersFactory.CreateTfaTokenParameters(),
                out SecurityToken validatedToken);

            string? tokenPurpose = principal
                .FindFirst(IdentityTokenConstants.TokenPurposeClaimType)?
                .Value;

            if (tokenPurpose != IdentityTokenConstants.TfaTokenPurpose)
            {
                _logger.LogDebug(
                    "TFA token validation failed because the token purpose claim was missing or invalid.");

                return Result.Failure<TfaTokenClaims>(
                    "TfaToken.InvalidPurpose",
                    "The TFA token is invalid.");
            }

            string? userIdClaim = principal
                .FindFirst(JwtRegisteredClaimNames.Sub)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogDebug(
                    "TFA token validation failed because the subject claim did not contain a valid user ID.");

                return Result.Failure<TfaTokenClaims>(
                    "TfaToken.InvalidSubject",
                    "The TFA token is invalid.",
                    StatusCodes.Status401Unauthorized);
            }

            return Result.Success(new TfaTokenClaims(userId));
        }
        catch (SecurityTokenExpiredException exception)
        {
            _logger.LogDebug(
                exception,
                "TFA token validation failed because the token has expired.");

            return Result.Failure<TfaTokenClaims>(
                "TfaToken.Expired",
                "The TFA token has expired.",
                StatusCodes.Status401Unauthorized);
        }
        catch (SecurityTokenException exception)
        {
            _logger.LogDebug(
                exception,
                "TFA token validation failed because the token is invalid.");

            return Result.Failure<TfaTokenClaims>(
                "TfaToken.Invalid",
                "The TFA token is invalid.",
                StatusCodes.Status401Unauthorized);
        }
        catch (ArgumentException exception)
        {
            _logger.LogDebug(
                exception,
                "TFA token validation failed because the token was malformed.");

            return Result.Failure<TfaTokenClaims>(
                "TfaToken.Malformed",
                "The TFA token is malformed.",
                StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// Creates and signs a JWT for the specified user and token purpose.
    /// </summary>
    /// <returns>
    /// The encoded JWT and its expiration time.
    /// </returns>
    private GeneratedJwtToken CreateJwtToken(
        Guid userId,
        string secretKey,
        int expirationInMinutes,
        string tokenUse)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expirationInMinutes);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUse);

        DateTimeOffset issuedAt = _timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = issuedAt.AddMinutes(expirationInMinutes);

        Claim[] claims =
        [
            new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId.ToString("D")),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("N")),

                new Claim(
                    IdentityTokenConstants.TokenPurposeClaimType,
                    tokenUse)
        ];

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),

            Issuer = _options.Issuer,
            Audience = _options.Audience,

            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,

            SigningCredentials = CreateSigningCredentials(secretKey)
        };

        string encodedToken =
            _tokenHandler.CreateEncodedJwt(tokenDescriptor);

        return new GeneratedJwtToken(
            Token: encodedToken,
            ExpiresAt: expiresAt);
    }

    /// <summary>
    /// Creates HMAC-SHA256 signing credentials from a Base64-encoded secret key.
    /// </summary>
    private static SigningCredentials CreateSigningCredentials(string secretKey)
    {
        byte[] keyBytes = Convert.FromBase64String(secretKey);

        var securityKey = new SymmetricSecurityKey(keyBytes);

        return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }
}
