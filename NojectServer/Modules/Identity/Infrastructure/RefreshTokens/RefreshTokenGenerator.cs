using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NojectServer.Modules.Identity.Application.RefreshTokens;

namespace NojectServer.Modules.Identity.Infrastructure.RefreshTokens;

/// <summary>
/// Generates cryptographically secure refresh tokens and computes hashes for token storage or comparison.
/// </summary>
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 64;

    /// <inheritdoc />
    public byte[] ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    /// <inheritdoc />
    public GeneratedRefreshToken Generate()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        string token = WebEncoders.Base64UrlEncode(randomBytes);

        return new GeneratedRefreshToken(token, ComputeHash(token));
    }
}
