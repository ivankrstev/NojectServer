using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Infrastructure.RefreshTokens;

namespace NojectServer.UnitTests.Identity.Infrastructure.RefreshTokens;

public sealed class RefreshTokenGeneratorTests
{
    private const int TokenSizeInBytes = 64;
    private const int HashLengthInBytes = 32;

    private readonly RefreshTokenGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsBase64UrlTokenWithExpectedDecodedLength()
    {
        GeneratedRefreshToken generatedToken = _generator.Generate();

        AssertBase64Url(generatedToken.PlainTextToken);
        byte[] decodedToken = WebEncoders.Base64UrlDecode(generatedToken.PlainTextToken);

        Assert.Equal(TokenSizeInBytes, decodedToken.Length);
    }

    [Fact]
    public void Generate_HashMatchesIndependentlyComputedSha256OfPlaintext()
    {
        GeneratedRefreshToken generatedToken = _generator.Generate();

        byte[] expectedHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(generatedToken.PlainTextToken));

        Assert.Equal(
            Convert.ToHexString(expectedHash),
            Convert.ToHexString(generatedToken.Hash));
        Assert.Equal(HashLengthInBytes, generatedToken.Hash.Length);
    }

    [Fact]
    public void ComputeHash_IsDeterministic()
    {
        const string token = "refresh-token-for-deterministic-hashing";

        byte[] firstHash = _generator.ComputeHash(token);
        byte[] secondHash = _generator.ComputeHash(token);

        Assert.Equal(Convert.ToHexString(firstHash), Convert.ToHexString(secondHash));
    }

    [Fact]
    public void ComputeHash_MatchesIndependentlyComputedSha256()
    {
        const string token = "refresh-token-for-independent-hash-check";

        byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        Assert.Equal(
            Convert.ToHexString(expectedHash),
            Convert.ToHexString(_generator.ComputeHash(token)));
    }

    [Fact]
    public void ComputeHash_WithNullToken_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _generator.ComputeHash(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void ComputeHash_WithBlankToken_ThrowsArgumentException(string token)
    {
        Assert.Throws<ArgumentException>(() => _generator.ComputeHash(token));
    }

    [Fact]
    public void Generate_ProducesDistinctTokensAcrossTwoSamples()
    {
        GeneratedRefreshToken first = _generator.Generate();
        GeneratedRefreshToken second = _generator.Generate();

        Assert.NotEqual(first.PlainTextToken, second.PlainTextToken);
    }

    private static void AssertBase64Url(string value)
    {
        Assert.NotEmpty(value);

        foreach (char character in value)
        {
            bool isBase64UrlCharacter =
                character is >= 'A' and <= 'Z'
                    or >= 'a' and <= 'z'
                    or >= '0' and <= '9'
                    or '-'
                    or '_';

            Assert.True(
                isBase64UrlCharacter,
                $"'{character}' is not a Base64url character.");
        }
    }
}
