using NojectServer.Services.Common.Implementations;
using System.Collections.Immutable;

namespace NojectServer.Tests.Services.Common;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    #region Create Password Hash Tests

    [Fact]
    public void CreatePasswordHash_ShouldCreateDifferentHashesForSamePassword()
    {
        // Arrange
        string password = "TestPassword123!";

        // Act
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash1, out ImmutableArray<byte> salt1);
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash2, out ImmutableArray<byte> salt2);

        // Assert
        Assert.False(hash1.IsDefaultOrEmpty);
        Assert.False(salt1.IsDefaultOrEmpty);
        Assert.False(hash2.IsDefaultOrEmpty);
        Assert.False(salt2.IsDefaultOrEmpty);

        // Different salts should be generated
        Assert.False(salt1.SequenceEqual(salt2));

        // Different hashes should be generated due to different salts
        Assert.False(hash1.SequenceEqual(hash2));
    }

    [Fact]
    public void CreatePasswordHash_ShouldGenerateNonEmptyHashAndSalt()
    {
        // Arrange
        string password = "TestPassword123!";

        // Act
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Assert
        Assert.False(hash.IsDefaultOrEmpty);
        Assert.False(salt.IsDefaultOrEmpty);
        Assert.True(hash.Length > 0);
        Assert.True(salt.Length > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreatePasswordHash_WithInvalidInput_ShouldStillGenerateHashAndSalt(string invalidPassword)
    {
        // Act & Assert (should not throw exception)
        _passwordService.CreatePasswordHash(invalidPassword, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Even for invalid inputs, hash and salt should be generated
        Assert.False(hash.IsDefaultOrEmpty);
        Assert.False(salt.IsDefaultOrEmpty);
        Assert.True(hash.Length > 0);
        Assert.True(salt.Length > 0);
    }

    #endregion

    #region Verify Password Hash Tests

    [Fact]
    public void VerifyPasswordHash_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, hash, salt);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPasswordHash_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        string correctPassword = "TestPassword123!";
        string incorrectPassword = "WrongPassword123!";
        _passwordService.CreatePasswordHash(correctPassword, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Act
        bool result = _passwordService.VerifyPasswordHash(incorrectPassword, hash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordHash_WithModifiedHash_ShouldReturnFalse()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Create a modified hash (can't modify immutable array directly)
        var hashArray = hash.ToArray();
        if (hashArray.Length > 0)
            hashArray[0] = (byte)(hashArray[0] + 1);
        var modifiedHash = ImmutableArray.Create(hashArray);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, modifiedHash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordHash_WithModifiedSalt_ShouldReturnFalse()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Create a modified salt (can't modify immutable array directly)
        var saltArray = salt.ToArray();
        if (saltArray.Length > 0)
            saltArray[0] = (byte)(saltArray[0] + 1);
        var modifiedSalt = ImmutableArray.Create(saltArray);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, hash, modifiedSalt);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPasswordHash_WithInvalidPassword_ShouldReturnFalse(string invalidPassword)
    {
        // Arrange
        string validPassword = "TestPassword123!";
        _passwordService.CreatePasswordHash(validPassword, out ImmutableArray<byte> hash, out ImmutableArray<byte> salt);

        // Act
        bool result = _passwordService.VerifyPasswordHash(invalidPassword, hash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordHash_WithEmptyHash_ShouldReturnFalse()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out _, out ImmutableArray<byte> salt);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, ImmutableArray<byte>.Empty, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordHash_WithEmptySalt_ShouldReturnFalse()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out ImmutableArray<byte> hash, out _);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, hash, ImmutableArray<byte>.Empty);

        // Assert
        Assert.False(result);
    }

    #endregion
}
