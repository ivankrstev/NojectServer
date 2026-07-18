using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Configurations.Tokens;
using NojectServer.Modules.Identity.Application.Interfaces;
using NojectServer.Modules.Identity.Application.Tfa;
using NojectServer.Modules.Identity.Infrastructure.Tokens;
using NojectServer.Utils.ResultPattern;

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
    JwtTokenValidationParametersFactory validationParametersFactory,
    TimeProvider timeProvider,
    ILogger<JwtTokenService> logger) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;
    private readonly TfaTokenOptions _tfaTokenOptions = tfaTokenOptions.Value;
    private readonly JwtTokenValidationParametersFactory _validationParametersFactory = validationParametersFactory;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<JwtTokenService> _logger = logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new()
    {
        MapInboundClaims = false
    };

    private const string TokenPurposeClaimType = "token_use";
    private const string AccessTokenPurpose = "access";
    private const string TfaTokenPurpose = "tfa";

    /// <inheritdoc/>
    public string CreateAccessToken(Guid userId)
    {
        return CreateJwtToken(
            userId,
            _accessTokenOptions.SecretKey,
            _accessTokenOptions.ExpirationInMinutes,
            AccessTokenPurpose);
    }

    /// <inheritdoc/>
    public string CreateTfaToken(Guid userId)
    {
        return CreateJwtToken(
            userId,
            _tfaTokenOptions.SecretKey,
            _tfaTokenOptions.ExpirationInMinutes,
            TfaTokenPurpose);
    }

    /// <inheritdoc/>
    public Result<TfaTokenPayload> ValidateTfaToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Result.Failure<TfaTokenPayload>(
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
                .FindFirst(TokenPurposeClaimType)?
                .Value;

            if (tokenPurpose != TfaTokenPurpose)
            {
                _logger.LogDebug(
                    "TFA token validation failed because the token purpose claim was missing or invalid.");

                return Result.Failure<TfaTokenPayload>(
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

                return Result.Failure<TfaTokenPayload>(
                    "TfaToken.InvalidSubject",
                    "The TFA token is invalid.",
                    StatusCodes.Status401Unauthorized);
            }

            return Result.Success(new TfaTokenPayload(userId));
        }
        catch (SecurityTokenExpiredException exception)
        {
            _logger.LogDebug(
                exception,
                "TFA token validation failed because the token has expired.");

            return Result.Failure<TfaTokenPayload>(
                "TfaToken.Expired",
                "The TFA token has expired.",
                StatusCodes.Status401Unauthorized);
        }
        catch (SecurityTokenException exception)
        {
            _logger.LogDebug(
                exception,
                "TFA token validation failed because the token is invalid.");

            return Result.Failure<TfaTokenPayload>(
                "TfaToken.Invalid",
                "The TFA token is invalid.",
                StatusCodes.Status401Unauthorized);
        }
        catch (ArgumentException exception)
        {
            _logger.LogDebug(
                exception,
                "TFA token validation failed because the token was malformed.");

            return Result.Failure<TfaTokenPayload>(
                "TfaToken.Malformed",
                "The TFA token is malformed.",
                StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// Creates a signed JWT token with the specified parameters.
    /// </summary>
    private string CreateJwtToken(Guid userId, string secretKey, int expirationInMinutes, string tokenUse)
    {
        DateTime utcNow = _timeProvider
            .GetUtcNow()
            .UtcDateTime;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,

            Subject = new ClaimsIdentity([
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId.ToString("D")),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("N")),

                new Claim(
                    TokenPurposeClaimType,
                    tokenUse)
            ]),

            IssuedAt = utcNow,
            NotBefore = utcNow,
            Expires = utcNow.AddMinutes(expirationInMinutes),

            SigningCredentials = CreateSigningCredentials(secretKey)
        };

        return _tokenHandler.CreateEncodedJwt(tokenDescriptor);
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
