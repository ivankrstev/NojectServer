using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NojectServer.Services.Auth.Implementations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NojectServer.Tests.Services.Auth;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly TokenService _tokenService;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenServiceTests()
    {
        // Setup mock configuration
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["JWTSecrets:AccessToken"]).Returns(
            "accessTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345");
        _mockConfiguration.Setup(c => c["JWTSecrets:RefreshToken"]).Returns(
            "refreshTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345");
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns(
            "tfaTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345678");

        _tokenService = new TokenService(_mockConfiguration.Object);
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    [Fact]
    public void CreateAccessToken_ShouldCreateValidToken()
    {
        // Arrange
        string email = "test@example.com";

        // Act
        string token = _tokenService.CreateAccessToken(email);

        // Assert
        Assert.NotNull(token);
        Assert.True(_tokenHandler.CanReadToken(token));

        JwtSecurityToken jwtToken = _tokenHandler.ReadJwtToken(token);

        // Verify claims
        Claim? claim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(claim);
        Assert.Equal(email, claim.Value);

        // Verify expiration (should be ~10 minutes)
        TimeSpan timeToExpiry = jwtToken.ValidTo - DateTime.UtcNow;
        Assert.True(timeToExpiry.TotalMinutes > 9 && timeToExpiry.TotalMinutes <= 10);
    }

    [Fact]
    public void CreateRefreshToken_ShouldCreateValidToken()
    {
        // Arrange
        string email = "test@example.com";

        // Act
        string token = _tokenService.CreateRefreshToken(email);

        // Assert
        Assert.NotNull(token);
        Assert.True(_tokenHandler.CanReadToken(token));

        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Verify claims
        var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(claim);
        Assert.Equal(email, claim.Value);

        // Verify expiration (should be ~14 days)
        TimeSpan timeToExpiry = jwtToken.ValidTo - DateTime.UtcNow;
        Assert.True(timeToExpiry.TotalDays > 13.9 && timeToExpiry.TotalDays <= 14);
    }

    [Fact]
    public void CreateTfaToken_ShouldCreateValidToken()
    {
        // Arrange
        string email = "test@example.com";

        // Act
        string token = _tokenService.CreateTfaToken(email);

        // Assert
        Assert.NotNull(token);
        Assert.True(_tokenHandler.CanReadToken(token));

        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Verify claims
        var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(claim);
        Assert.Equal(email, claim.Value);

        // Verify expiration (should be ~2 minutes)
        TimeSpan timeToExpiry = jwtToken.ValidTo - DateTime.UtcNow;
        Assert.True(timeToExpiry.TotalMinutes > 1.9 && timeToExpiry.TotalMinutes <= 2);
    }

    [Fact]
    public void GetTfaTokenValidationParameters_ShouldReturnValidParameters()
    {
        // Act
        var parameters = _tokenService.GetTfaTokenValidationParameters();

        // Assert
        Assert.NotNull(parameters);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.False(parameters.ValidateIssuer);
        Assert.False(parameters.ValidateAudience);
        Assert.True(parameters.ValidateLifetime);
        Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);

        // Verify the signing key was created correctly
        var securityKey = parameters.IssuerSigningKey as SymmetricSecurityKey;
        Assert.NotNull(securityKey);

        // We can't directly compare the key bytes, but we can verify it's the expected type
        Assert.IsType<SymmetricSecurityKey>(parameters.IssuerSigningKey);
    }

    /// <summary>
    /// Tests that verify proper exception handling when JWT configuration is missing.
    /// These tests ensure the service fails early with clear error messages when
    /// required security settings are not configured properly.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:AccessToken"]).Returns((string)null!);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateAccessToken("test@example.com"));
        Assert.Contains("JWTSecrets:AccessToken", exception.Message);
    }

    [Fact]
    public void CreateRefreshToken_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:RefreshToken"]).Returns((string)null!);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateRefreshToken("test@example.com"));
        Assert.Contains("JWTSecrets:RefreshToken", exception.Message);
    }

    [Fact]
    public void CreateTfaToken_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns((string)null!);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateTfaToken("test@example.com"));
        Assert.Contains("JWTSecrets:TfaToken", exception.Message);
    }

    [Fact]
    public void GetTfaTokenValidationParameters_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns((string)null!);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => tokenService.GetTfaTokenValidationParameters());
        Assert.Contains("JWTSecrets:TfaToken", exception.Message);
    }

    /// <summary>
    /// Tests that verify behavior when configuration values are empty strings.
    /// Ensures proper validation of key material to prevent security issues.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:AccessToken"]).Returns(string.Empty);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.CreateAccessToken("test@example.com"));
        Assert.Contains("The key size is 0 bytes", exception.Message);
    }

    [Fact]
    public void CreateRefreshToken_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:RefreshToken"]).Returns(string.Empty);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.CreateRefreshToken("test@example.com"));
        Assert.Contains("The key size is 0 bytes", exception.Message);
    }

    [Fact]
    public void CreateTfaToken_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns(string.Empty);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.CreateTfaToken("test@example.com"));
        Assert.Contains("The key size is 0 bytes", exception.Message);
    }

    [Fact]
    public void GetTfaTokenValidationParameters_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns(string.Empty);
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.GetTfaTokenValidationParameters());
        Assert.Contains("The key size is 0 bytes", exception.Message);
    }

    /// <summary>
    /// Tests for key length requirements when using HMAC-SHA512.
    /// Verifies that cryptographically insecure short keys are rejected.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithInvalidKeyLength_ThrowsArgumentException()
    {
        // Arrange - Key that's too short for HMAC-SHA512
        _mockConfiguration.Setup(c => c["JWTSecrets:AccessToken"]).Returns("short-key");
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateAccessToken("test@example.com"));
        Assert.Contains("IDX10720", exception.Message); // Key size error
    }

    [Fact]
    public void CreateRefreshToken_WithInvalidKeyLength_ThrowsArgumentException()
    {
        // Arrange - Key that's too short for HMAC-SHA512
        _mockConfiguration.Setup(c => c["JWTSecrets:RefreshToken"]).Returns("short-key");
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateRefreshToken("test@example.com"));
        Assert.Contains("IDX10720", exception.Message); // Key size error
    }

    [Fact]
    public void CreateTfaToken_WithInvalidKeyLength_ThrowsArgumentException()
    {
        // Arrange - Key that's too short for HMAC-SHA512
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns("short-key");
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateTfaToken("test@example.com"));
        Assert.Contains("IDX10720", exception.Message); // Key size error
    }

    /// <summary>
    /// Tests for input parameter validation.
    /// Ensures null email addresses are properly rejected with appropriate exceptions.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithNullEmail_ThrowsArgumentNullException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:AccessToken"]).Returns(
            "accessTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345");
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tokenService.CreateAccessToken(null!));
    }

    [Fact]
    public void CreateRefreshToken_WithNullEmail_ThrowsArgumentNullException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:RefreshToken"]).Returns(
            "refreshTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345");
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tokenService.CreateRefreshToken(null!));
    }

    [Fact]
    public void CreateTfaToken_WithNullEmail_ThrowsArgumentNullException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JWTSecrets:TfaToken"]).Returns(
            "tfaTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345678");
        var tokenService = new TokenService(_mockConfiguration.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tokenService.CreateTfaToken(null!));
    }
}