using NojectServer.Services.Common.Implementations;

namespace NojectServer.Tests.Services.Common;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void CreatePasswordHash_ShouldCreateDifferentHashesForSamePassword()
    {
        // Arrange
        string password = "TestPassword123!";

        // Act
        _passwordService.CreatePasswordHash(password, out byte[] hash1, out byte[] salt1);
        _passwordService.CreatePasswordHash(password, out byte[] hash2, out byte[] salt2);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotNull(salt1);
        Assert.NotNull(hash2);
        Assert.NotNull(salt2);
        Assert.NotEmpty(hash1);
        Assert.NotEmpty(salt1);
        Assert.NotEmpty(hash2);
        Assert.NotEmpty(salt2);

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
        _passwordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

        // Assert
        Assert.NotNull(hash);
        Assert.NotNull(salt);
        Assert.NotEmpty(hash);
        Assert.NotEmpty(salt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreatePasswordHash_WithInvalidInput_ShouldStillGenerateHashAndSalt(string invalidPassword)
    {
        // Act & Assert (should not throw exception)
        _passwordService.CreatePasswordHash(invalidPassword, out byte[] hash, out byte[] salt);

        // Even for invalid inputs, hash and salt should be generated
        Assert.NotNull(hash);
        Assert.NotNull(salt);
        Assert.NotEmpty(hash);
        Assert.NotEmpty(salt);
    }

    [Fact]
    public void VerifyPasswordHash_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

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
        _passwordService.CreatePasswordHash(correctPassword, out byte[] hash, out byte[] salt);

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
        _passwordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

        // Modify one byte in the hash
        if (hash.Length > 0)
            hash[0] = (byte)(hash[0] + 1);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, hash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordHash_WithModifiedSalt_ShouldReturnFalse()
    {
        // Arrange
        string password = "TestPassword123!";
        _passwordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

        // Modify one byte in the salt
        if (salt.Length > 0)
            salt[0] = (byte)(salt[0] + 1);

        // Act
        bool result = _passwordService.VerifyPasswordHash(password, hash, salt);

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
        _passwordService.CreatePasswordHash(validPassword, out byte[] hash, out byte[] salt);

        // Act
        bool result = _passwordService.VerifyPasswordHash(invalidPassword, hash, salt);

        // Assert
        Assert.False(result);
    }
}
