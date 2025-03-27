using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NojectServer.Configurations;
using NojectServer.Services.Auth.Implementations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NojectServer.Tests.Services.Auth;

public class TokenServiceTests
{
    private readonly Mock<IOptions<JwtTokenOptions>> _mockOptions;
    private readonly TokenService _tokenService;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly JwtTokenOptions _jwtTokenOptions;

    public TokenServiceTests()
    {
        // Setup JwtTokenOptions with test values
        _jwtTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = "accessTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345",
                ExpirationInSeconds = 600 // 10 minutes
            },
            Refresh = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = "refreshTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345",
                ExpirationInSeconds = 1209600 // 14 days
            },
            Tfa = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = "tfaTokenSecretWithAtLeast64BytesRequiredForHmacSha512Algorithm_ThisIsLongEnoughToWork12345678",
                ExpirationInSeconds = 120 // 2 minutes
            }
        };

        // Setup mock options
        _mockOptions = new Mock<IOptions<JwtTokenOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_jwtTokenOptions);

        _tokenService = new TokenService(_mockOptions.Object);
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
        Assert.True(parameters.ValidateIssuer);
        Assert.True(parameters.ValidateAudience);
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
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = null!, // Invalid configuration
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = _jwtTokenOptions.Tfa
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.CreateAccessToken("test@example.com"));
    }

    [Fact]
    public void CreateRefreshToken_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = null!, // Invalid configuration
            Tfa = _jwtTokenOptions.Tfa
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.CreateRefreshToken("test@example.com"));
    }

    [Fact]
    public void CreateTfaToken_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = null! // Invalid configuration
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.CreateTfaToken("test@example.com"));
    }

    [Fact]
    public void GetTfaTokenValidationParameters_WithMissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = null! // Invalid configuration
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.GetTfaTokenValidationParameters());
    }

    /// <summary>
    /// Tests that verify behavior when configuration values are empty strings.
    /// Ensures proper validation of key material to prevent security issues.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = string.Empty,
                ExpirationInSeconds = 600
            },
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = _jwtTokenOptions.Tfa
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.CreateAccessToken("test@example.com"));
        Assert.Contains("JWT Access token secret key", exception.Message);
    }

    [Fact]
    public void CreateRefreshToken_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = string.Empty,
                ExpirationInSeconds = 1209600
            },
            Tfa = _jwtTokenOptions.Tfa
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.CreateRefreshToken("test@example.com"));
        Assert.Contains("JWT Refresh token secret key", exception.Message);
    }

    [Fact]
    public void CreateTfaToken_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = string.Empty,
                ExpirationInSeconds = 120
            }
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.CreateTfaToken("test@example.com"));
        Assert.Contains("JWT Tfa token secret key", exception.Message);
    }

    [Fact]
    public void GetTfaTokenValidationParameters_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = string.Empty,
                ExpirationInSeconds = 120
            }
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tokenService.GetTfaTokenValidationParameters());
        Assert.Contains("JWT Tfa token secret key", exception.Message);
    }

    /// <summary>
    /// Tests for key length requirements when using HMAC-SHA512.
    /// Verifies that cryptographically insecure short keys are rejected.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithInvalidKeyLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange - Key that's too short for HMAC-SHA512
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = "short-key",
                ExpirationInSeconds = 600
            },
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = _jwtTokenOptions.Tfa
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateAccessToken("test@example.com"));
        Assert.Contains("IDX10653", exception.Message); // Key size error
    }

    [Fact]
    public void CreateRefreshToken_WithInvalidKeyLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange - Key that's too short for HMAC-SHA512
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = "short-key",
                ExpirationInSeconds = 1209600
            },
            Tfa = _jwtTokenOptions.Tfa
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateRefreshToken("test@example.com"));
        Assert.Contains("IDX10653", exception.Message); // Key size error
    }

    [Fact]
    public void CreateTfaToken_WithInvalidKeyLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange - Key that's too short for HMAC-SHA512
        var invalidOptions = new Mock<IOptions<JwtTokenOptions>>();
        var invalidTokenOptions = new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Access = _jwtTokenOptions.Access,
            Refresh = _jwtTokenOptions.Refresh,
            Tfa = new JwtTokenOptions.JwtSigningCredentials
            {
                SecretKey = "short-key",
                ExpirationInSeconds = 120
            }
        };

        invalidOptions.Setup(o => o.Value).Returns(invalidTokenOptions);
        var tokenService = new TokenService(invalidOptions.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateTfaToken("test@example.com"));
        Assert.Contains("IDX10653", exception.Message); // Key size error
    }

    /// <summary>
    /// Tests for input parameter validation.
    /// Ensures null email addresses are properly rejected with appropriate exceptions.
    /// </summary>
    [Fact]
    public void CreateAccessToken_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _tokenService.CreateAccessToken(null!));
    }

    [Fact]
    public void CreateRefreshToken_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _tokenService.CreateRefreshToken(null!));
    }

    [Fact]
    public void CreateTfaToken_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _tokenService.CreateTfaToken(null!));
    }
}