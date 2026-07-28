using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NojectServer.Modules.Identity.Application.Tokens;

namespace NojectServer.Modules.Identity.Infrastructure.Tokens;

/// <summary>
/// Generates opaque tokens from secure random bytes and computes their SHA-256 hashes.
/// </summary>
public class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    private const int MinimumSizeInBytes = 32;

    /// <inheritdoc />
    public GeneratedToken Generate(int sizeInBytes)
    {
        if (sizeInBytes < MinimumSizeInBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeInBytes),
                $"Token size must be at least {MinimumSizeInBytes} bytes.");
        }

        byte[] randomBytes = RandomNumberGenerator.GetBytes(sizeInBytes);

        string plainText = WebEncoders.Base64UrlEncode(randomBytes);

        return new GeneratedToken(
            PlainText: plainText,
            Hash: ComputeHash(plainText));
    }

    /// <inheritdoc />
    public byte[] ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return SHA256.HashData(
            Encoding.UTF8.GetBytes(token));
    }
}
