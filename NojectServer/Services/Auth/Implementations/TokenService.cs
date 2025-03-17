using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Configurations;
using NojectServer.Services.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NojectServer.Services.Auth.Implementations;

public class TokenService(IOptions<JwtTokenOptions> options) : ITokenService
{
    private readonly JwtTokenOptions _jwtTokenOptions = options.Value;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public string CreateAccessToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");

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

    public string CreateRefreshToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");

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

    public string CreateTfaToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");

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

    public TokenValidationParameters GetTfaTokenValidationParameters()
    {
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
}
