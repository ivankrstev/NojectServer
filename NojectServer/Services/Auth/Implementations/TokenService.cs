using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Configurations;
using NojectServer.Services.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NojectServer.Services.Auth.Implementations;

/// <summary>
/// Implementation of the ITokenService interface that generates and validates JWT tokens
/// for authentication and authorization purposes.
///
/// This service handles three token types:
/// - Access tokens for API authentication
/// - Refresh tokens for obtaining new access tokens
/// - Two-factor authentication (TFA) tokens for multi-factor authentication flows
///
/// Each token type has its own configuration including secret key and expiration time,
/// loaded from application settings.
/// </summary>
public class TokenService(IOptions<JwtTokenOptions> options) : ITokenService
{
    private readonly JwtTokenOptions _jwtTokenOptions = options.Value;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// Creates a JWT access token containing the user's email as a claim.
    /// The token is signed with the access token secret key and includes configured
    /// issuer, audience, and expiration time.
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when JWT configuration is incomplete or invalid</exception>
    /// <exception cref="ArgumentException">Thrown when required JWT settings are empty or invalid</exception>
    public string CreateAccessToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");

        try
        {
            if (_jwtTokenOptions.Access == null)
                throw new InvalidOperationException("JWT Access token configuration is missing");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Access.SecretKey))
                throw new ArgumentException("JWT Access token secret key cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Issuer))
                throw new ArgumentException("JWT issuer cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Audience))
                throw new ArgumentException("JWT audience cannot be empty");

            // Create a list of claims with the user's email
            List<Claim> claims = [new Claim(ClaimTypes.Name, email)];

            // Create a new symmetric security key from the secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenOptions.Access.SecretKey));

            // Create new signing credentials with the key and the HMAC-SHA512 signature algorithm
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // Create a new JWT token with the claims, issuer, audience, expiration time and credentials
            var token = new JwtSecurityToken(
                issuer: _jwtTokenOptions.Issuer,
                audience: _jwtTokenOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(_jwtTokenOptions.Access.ExpirationInSeconds),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }
        catch (NullReferenceException)
        {
            // Handle the exception when a required property is null
            throw new InvalidOperationException("JWT configuration is incomplete. Make sure Access token settings are properly configured.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("The key size is"))
        {
            // Handle the exception when the key size is invalid, thrown by the SymmetricSecurityKey constructor
            throw new InvalidOperationException("JWT Access token secret key has invalid length", ex);
        }
    }

    /// <summary>
    /// Creates a JWT refresh token containing the user's email as a claim.
    /// The token is signed with the refresh token secret key and includes configured
    /// issuer, audience, and expiration time (typically longer than access tokens).
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when JWT configuration is incomplete or invalid</exception>
    /// <exception cref="ArgumentException">Thrown when required JWT settings are empty or invalid</exception>
    public string CreateRefreshToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");

        try
        {
            if (_jwtTokenOptions.Refresh == null)
                throw new InvalidOperationException("JWT Refresh token configuration is missing");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Refresh.SecretKey))
                throw new ArgumentException("JWT Refresh token secret key cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Issuer))
                throw new ArgumentException("JWT issuer cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Audience))
                throw new ArgumentException("JWT audience cannot be empty");

            // Create a list of claims with the user's email
            List<Claim> claims = [new Claim(ClaimTypes.Name, email)];

            // Create a new symmetric security key from the secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenOptions.Refresh.SecretKey));

            // Create new signing credentials with the key and the HMAC-SHA512 signature algorithm
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // Create a new JWT token with the claims, issuer, audience, expiration time and credentials
            var token = new JwtSecurityToken(
                issuer: _jwtTokenOptions.Issuer,
                audience: _jwtTokenOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(_jwtTokenOptions.Refresh.ExpirationInSeconds),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }
        catch (NullReferenceException)
        {
            // Handle the exception when a required property is null
            throw new InvalidOperationException("JWT configuration is incomplete. Make sure Refresh token settings are properly configured.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("The key size is"))
        {
            // Handle the exception when the key size is invalid, thrown by the SymmetricSecurityKey constructor
            throw new InvalidOperationException("JWT Refresh token secret key has invalid length", ex);
        }
    }

    /// <summary>
    /// Creates a JWT token for two-factor authentication containing the user's email as a claim.
    /// The token is signed with the TFA token secret key and includes configured issuer, audience,
    /// and a short expiration time suitable for TFA flows.
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when JWT configuration is incomplete or invalid</exception>
    /// <exception cref="ArgumentException">Thrown when required JWT settings are empty or invalid</exception>
    public string CreateTfaToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");

        try
        {
            if (_jwtTokenOptions.Tfa == null)
                throw new InvalidOperationException("JWT Tfa token configuration is missing");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Tfa.SecretKey))
                throw new ArgumentException("JWT Tfa token secret key cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Issuer))
                throw new ArgumentException("JWT issuer cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Audience))
                throw new ArgumentException("JWT audience cannot be empty");

            // Create a list of claims with the user's email
            List<Claim> claims = [new Claim(ClaimTypes.Name, email)];

            // Create a new symmetric security key from the secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenOptions.Tfa.SecretKey));

            // Create new signing credentials with the key and the HMAC-SHA512 signature algorithm
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // Create a new JWT token with the claims, an expiration time of 2 minutes, and the signing credentials
            var token = new JwtSecurityToken(
                issuer: _jwtTokenOptions.Issuer,
                audience: _jwtTokenOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(_jwtTokenOptions.Tfa.ExpirationInSeconds),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }
        catch (NullReferenceException)
        {
            throw new InvalidOperationException("JWT configuration is incomplete. Make sure Tfa token settings are properly configured.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("The key size is"))
        {
            throw new InvalidOperationException("JWT Tfa token secret key has invalid length", ex);
        }
    }

    /// <summary>
    /// Provides the validation parameters required to verify TFA tokens.
    /// These parameters define how a TFA token should be validated, including issuer, audience,
    /// lifetime, and signing key verification requirements.
    /// </summary>
    /// <returns>TokenValidationParameters configured for TFA token validation</returns>
    /// <exception cref="InvalidOperationException">Thrown when JWT configuration is incomplete or invalid</exception>
    /// <exception cref="ArgumentException">Thrown when required JWT settings are empty or invalid</exception>
    public TokenValidationParameters GetTfaTokenValidationParameters()
    {
        try
        {
            if (_jwtTokenOptions.Tfa == null)
                throw new InvalidOperationException("JWT Tfa token configuration is missing");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Tfa.SecretKey))
                throw new ArgumentException("JWT Tfa token secret key cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Issuer))
                throw new ArgumentException("JWT issuer cannot be empty");

            if (string.IsNullOrEmpty(_jwtTokenOptions.Audience))
                throw new ArgumentException("JWT audience cannot be empty");

            // Create and return new token validation parameters
            return new()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenOptions.Tfa.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwtTokenOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtTokenOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }
        catch (NullReferenceException)
        {
            throw new InvalidOperationException("JWT configuration is incomplete. Make sure Tfa token settings are properly configured.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("The key size is"))
        {
            throw new InvalidOperationException("JWT Tfa token secret key has invalid length", ex);
        }
    }
}
