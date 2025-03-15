using Microsoft.IdentityModel.Tokens;
using NojectServer.Services.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NojectServer.Services.Auth.Implementations;

public class TokenService(IConfiguration config) : ITokenService
{
    private readonly IConfiguration _config = config;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    // HMAC-SHA512 requires at least 512 bits (64 bytes)
    private const int MinKeyLengthInBytes = 64;

    public string CreateAccessToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");
        // Get the secret key from the configuration, or throw an exception if it's missing
        string? secretKey = _config["JWTSecrets:AccessToken"] ?? throw new InvalidOperationException("JWT configuration missing: JWTSecrets:AccessToken");
        // Check if the secret key is empty, and throw an exception if it is
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("The key size is 0 bytes", nameof(secretKey));
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        // Check minimum key length for HMAC-SHA512
        if (keyBytes.Length < MinKeyLengthInBytes)
            throw new ArgumentOutOfRangeException(nameof(secretKey),
                $"IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'http://www.w3.org/2001/04/xmldsig-more#hmac-sha512', " +
                $"the key size must be greater than: '512' bits, key has '{keyBytes.Length * 8}' bits.");
        // Create a list of claims with the user's email
        List<Claim> claims = [new Claim(ClaimTypes.Name, email)];
        // Create a new symmetric security key from the secret key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:AccessToken"]!));
        // Create new signing credentials with the key and the HMAC-SHA512 signature algorithm
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        // Create a new JWT token with the claims, an expiration time of 10 minutes, and the signing credentials
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return _tokenHandler.WriteToken(token);
    }

    public string CreateRefreshToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");
        // Get the secret key from the configuration, or throw an exception if it's missing
        string? secretKey = _config["JWTSecrets:RefreshToken"] ?? throw new InvalidOperationException("JWT configuration missing: JWTSecrets:RefreshToken");
        // Check if the secret key is empty, and throw an exception if it is
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("The key size is 0 bytes", nameof(secretKey));
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        // Check minimum key length for HMAC-SHA512
        if (keyBytes.Length < MinKeyLengthInBytes)
            throw new ArgumentOutOfRangeException(nameof(secretKey),
                $"IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'http://www.w3.org/2001/04/xmldsig-more#hmac-sha512', " +
                $"the key size must be greater than: '512' bits, key has '{keyBytes.Length * 8}' bits.");
        // Create a list of claims with the user's email
        List<Claim> claims = [new Claim(ClaimTypes.Name, email)];
        // Create a new symmetric security key from the secret key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:RefreshToken"]!));
        // Create new signing credentials with the key and the HMAC-SHA512 signature algorithm
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        // Create a new JWT token with the claims, an expiration time of 14 days, and the signing credentials
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(14),
            signingCredentials: credentials);
        return _tokenHandler.WriteToken(token);
    }

    public string CreateTfaToken(string email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null");
        // Get the secret key from the configuration, or throw an exception if it's missing
        string? secretKey = _config["JWTSecrets:TfaToken"] ?? throw new InvalidOperationException("JWT configuration missing: JWTSecrets:TfaToken");
        // Check if the secret key is empty, and throw an exception if it is
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("The key size is 0 bytes", nameof(secretKey));
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        // Check minimum key length for HMAC-SHA512
        if (keyBytes.Length < MinKeyLengthInBytes)
            throw new ArgumentOutOfRangeException(nameof(secretKey),
                $"IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'http://www.w3.org/2001/04/xmldsig-more#hmac-sha512', " +
                $"the key size must be greater than: '512' bits, key has '{keyBytes.Length * 8}' bits.");
        // Create a list of claims with the user's email
        List<Claim> claims = [new Claim(ClaimTypes.Name, email)];
        // Create a new symmetric security key from the secret key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:TfaToken"]!));
        // Create new signing credentials with the key and the HMAC-SHA512 signature algorithm
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        // Create a new JWT token with the claims, an expiration time of 2 minutes, and the signing credentials
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(2),
            signingCredentials: credentials);
        return _tokenHandler.WriteToken(token);
    }

    public TokenValidationParameters GetTfaTokenValidationParameters()
    {
        // Get the secret key from the configuration, or throw an exception if it's missing
        string? secretKey = _config["JWTSecrets:TfaToken"] ?? throw new InvalidOperationException("JWT configuration missing: JWTSecrets:TfaToken");
        // Check if the secret key is empty, and throw an exception if it is
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("The key size is 0 bytes", nameof(secretKey));
        // Create and return new token validation parameters with the secret key and other settings
        return new()
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:TfaToken"]!)),
            ClockSkew = TimeSpan.Zero
        };
    }
}