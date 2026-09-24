using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NojectServer.Modules.Identity.Application.Tokens;
using NojectServer.Modules.Identity.Infrastructure.Tokens;

namespace NojectServer.UnitTests.Identity.Infrastructure.Tokens;

public sealed class OpaqueTokenGeneratorTests
{
    private const int MinimumTokenSizeInBytes = 32;
    private const int HashLengthInBytes = 32;

    private readonly OpaqueTokenGenerator _generator = new();

    [Theory]
    [InlineData(MinimumTokenSizeInBytes)]
    [InlineData(MinimumTokenSizeInBytes + 1)]
    [InlineData(64)]
    public void Generate_WithSupportedSize_ReturnsBase64UrlTokenWithExpectedDecodedLength(
        int sizeInBytes)
    {
        GeneratedToken generatedToken = _generator.Generate(sizeInBytes);

        AssertBase64Url(generatedToken.PlainText);
        byte[] decodedToken = WebEncoders.Base64UrlDecode(generatedToken.PlainText);

        Assert.Equal(sizeInBytes, decodedToken.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(MinimumTokenSizeInBytes - 1)]
    public void Generate_WithSizeBelowMinimum_ThrowsArgumentOutOfRangeException(
        int sizeInBytes)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => _generator.Generate(sizeInBytes));

        Assert.Equal("sizeInBytes", exception.ParamName);
    }

    [Fact]
    public void Generate_HashMatchesIndependentlyComputedSha256OfPlaintext()
    {
        GeneratedToken generatedToken = _generator.Generate(MinimumTokenSizeInBytes);

        byte[] expectedHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(generatedToken.PlainText));

        Assert.Equal(
            Convert.ToHexString(expectedHash),
            Convert.ToHexString(generatedToken.Hash));
        Assert.Equal(HashLengthInBytes, generatedToken.Hash.Length);
    }

    [Fact]
    public void ComputeHash_IsDeterministic()
    {
        const string token = "opaque-token-for-deterministic-hashing";

        byte[] firstHash = _generator.ComputeHash(token);
        byte[] secondHash = _generator.ComputeHash(token);

        Assert.Equal(Convert.ToHexString(firstHash), Convert.ToHexString(secondHash));
    }

    [Fact]
    public void ComputeHash_MatchesIndependentlyComputedSha256()
    {
        const string token = "opaque-token-for-independent-hash-check";

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
        GeneratedToken first = _generator.Generate(MinimumTokenSizeInBytes);
        GeneratedToken second = _generator.Generate(MinimumTokenSizeInBytes);

        Assert.NotEqual(first.PlainText, second.PlainText);
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
