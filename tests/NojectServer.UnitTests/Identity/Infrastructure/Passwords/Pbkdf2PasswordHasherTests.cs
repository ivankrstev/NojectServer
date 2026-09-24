using System.Text;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Infrastructure.Passwords;

namespace NojectServer.UnitTests.Identity.Infrastructure.Passwords;

public sealed class Pbkdf2PasswordHasherTests
{
    private const int ExpectedHashLength = 32;
    private const int ExpectedSaltLength = 32;

    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsHashAndSaltWithExpectedLengths()
    {
        HashedPassword hashedPassword = _hasher.Hash("correct-password");

        Assert.Equal(ExpectedHashLength, hashedPassword.Hash.Length);
        Assert.Equal(ExpectedSaltLength, hashedPassword.Salt.Length);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        HashedPassword hashedPassword = _hasher.Hash("correct-password");

        bool result = _hasher.Verify(
            "correct-password",
            hashedPassword.Hash,
            hashedPassword.Salt);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        HashedPassword hashedPassword = _hasher.Hash("correct-password");

        bool result = _hasher.Verify(
            "wrong-password",
            hashedPassword.Hash,
            hashedPassword.Salt);

        Assert.False(result);
    }

    [Fact]
    public void HashingTheSamePasswordTwice_ProducesDifferentSalts()
    {
        HashedPassword first = _hasher.Hash("correct-password");
        HashedPassword second = _hasher.Hash("correct-password");

        Assert.NotEqual(
            Convert.ToHexString(first.Salt),
            Convert.ToHexString(second.Salt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ExpectedHashLength - 1)]
    [InlineData(ExpectedHashLength + 1)]
    public void Verify_WithInvalidHashLength_ReturnsFalse(int hashLength)
    {
        HashedPassword hashedPassword = _hasher.Hash("correct-password");

        bool result = _hasher.Verify(
            "correct-password",
            new byte[hashLength],
            hashedPassword.Salt);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ExpectedSaltLength - 1)]
    [InlineData(ExpectedSaltLength + 1)]
    public void Verify_WithInvalidSaltLength_ReturnsFalse(int saltLength)
    {
        HashedPassword hashedPassword = _hasher.Hash("correct-password");

        bool result = _hasher.Verify(
            "correct-password",
            hashedPassword.Hash,
            new byte[saltLength]);

        Assert.False(result);
    }

    [Fact]
    public void Verify_WithIndependentlyCalculatedFixture_ReturnsTrue()
    {
        const string password = "password-for-fixture";
        byte[] salt = Encoding.UTF8.GetBytes(
            "0123456789abcdef0123456789abcdef");
        byte[] independentlyCalculatedHash = Convert.FromHexString(
            "9F225B9A1B901B1FD023EF9F335855E94A3AC03C60171097058059970D4D1E85");

        bool result = _hasher.Verify(
            password,
            independentlyCalculatedHash,
            salt);

        Assert.True(result);
    }

    [Fact]
    public void Hash_WithNullPassword_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.Hash(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Hash_WithBlankPassword_ThrowsArgumentException(string password)
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(password));
    }

    [Fact]
    public void Verify_WithNullPassword_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.Verify(
            null!,
            new byte[ExpectedHashLength],
            new byte[ExpectedSaltLength]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Verify_WithBlankPassword_ThrowsArgumentException(string password)
    {
        Assert.Throws<ArgumentException>(() => _hasher.Verify(
            password,
            new byte[ExpectedHashLength],
            new byte[ExpectedSaltLength]));
    }

    [Fact]
    public void Verify_WithNullHashArray_ReturnsFalse()
    {
        byte[]? hash = null;

        bool result = _hasher.Verify(
            "correct-password",
            hash,
            new byte[ExpectedSaltLength]);

        Assert.False(result);
    }

    [Fact]
    public void Verify_WithNullSaltArray_ReturnsFalse()
    {
        byte[]? salt = null;

        bool result = _hasher.Verify(
            "correct-password",
            new byte[ExpectedHashLength],
            salt);

        Assert.False(result);
    }
}
