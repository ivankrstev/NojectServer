using Microsoft.Extensions.Configuration;
using Moq;
using Moq.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Auth.Validation.Interfaces;
using NojectServer.Utils.ResultPattern;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Auth;

public class TwoFactorAuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<DataContext> _mockDataContext;
    private readonly Mock<ITotpValidator> _mockTotpValidator;
    private readonly TwoFactorAuthService _service;

    public TwoFactorAuthServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockDataContext = new Mock<DataContext>();
        _mockTotpValidator = new Mock<ITotpValidator>();
        _service = new TwoFactorAuthService(
            _mockConfig.Object,
            _mockDataContext.Object,
            _mockTotpValidator.Object);

        // Configure default configuration values
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(x => x.Value).Returns("Noject");
        _mockConfig.Setup(x => x["AppName"]).Returns("Noject");
    }

    #region DisableTwoFactorAsync Tests

    [Fact]
    public async Task DisableTwoFactorAsync_UserNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var users = new List<User>();
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.DisableTwoFactorAsync("nonexistent@email.com", "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_TwoFactorAlreadyDisabled_ReturnsBadRequestFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorEnabled = false
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.DisableTwoFactorAsync("test@example.com", "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_InvalidCode_ReturnsUnauthorizedFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorEnabled = true,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP" // This is a valid Base32 string for testing
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.DisableTwoFactorAsync("test@example.com", "111111"); // Invalid code

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_ValidCode_DisablesTwoFactorAndReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorEnabled = true,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP"
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        _mockDataContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockTotpValidator.Setup(v => v.ValidateCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.DisableTwoFactorAsync("test@example.com", "123456");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal("2FA disabled successfully.", successResult.Value);

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorSecretKey);
        _mockDataContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region EnableTwoFactorAsync Tests

    [Fact]
    public async Task EnableTwoFactorAsync_UserNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var users = new List<User>();
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.EnableTwoFactorAsync("nonexistent@email.com", "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_NoSecretKey_ReturnsBadRequestFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorSecretKey = null
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.EnableTwoFactorAsync("test@example.com", "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_InvalidCode_ReturnsUnauthorizedFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP",
            TwoFactorEnabled = false
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.EnableTwoFactorAsync("test@example.com", "111111"); // Invalid code

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_ValidCode_EnablesTwoFactorAndReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP",
            TwoFactorEnabled = false
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        _mockDataContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockTotpValidator.Setup(v => v.ValidateCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.EnableTwoFactorAsync("test@example.com", "123456");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal("2FA enabled successfully.", successResult.Value);

        Assert.True(user.TwoFactorEnabled);
        Assert.NotNull(user.TwoFactorSecretKey);
        _mockDataContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GenerateSetupCodeAsync Tests

    [Fact]
    public async Task GenerateSetupCodeAsync_UserNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var users = new List<User>();
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.GenerateSetupCodeAsync("nonexistent@email.com");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<TwoFactorSetupResult>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_TwoFactorAlreadyEnabled_ReturnsBadRequestFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorEnabled = true
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.GenerateSetupCodeAsync("test@example.com");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<TwoFactorSetupResult>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_Success_GeneratesCodeAndReturnsSetupData()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorEnabled = false
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);
        _mockDataContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GenerateSetupCodeAsync("test@example.com");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<TwoFactorSetupResult>>(result);
        Assert.NotNull(successResult.Value);
        Assert.NotEmpty(successResult.Value.ManualKey);
        Assert.NotEmpty(successResult.Value.QrCodeImageUrl);
        Assert.Contains("otpauth://totp/", successResult.Value.QrCodeImageUrl);
        Assert.Contains("test%40example.com", successResult.Value.QrCodeImageUrl);
        Assert.Equal(successResult.Value.ManualKey, user.TwoFactorSecretKey);
        _mockDataContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ValidateTwoFactorCodeAsync Tests

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_UserNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var users = new List<User>();
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync("nonexistent@email.com", "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<bool>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_NoSecretKey_ReturnsBadRequestFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorSecretKey = null
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync("test@example.com", "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<bool>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_WithValidCode_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP"
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Setup the validator to return true (valid code)
        _mockTotpValidator.Setup(v => v.ValidateCode("JBSWY3DPEHPK3PXP", "123456"))
            .Returns(true);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync("test@example.com", "123456");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<bool>>(result);
        Assert.True(successResult.Value);
    }

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_WithInvalidCode_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP"
        };

        var users = new List<User> { user };
        _mockDataContext.Setup(c => c.Users).ReturnsDbSet(users);

        // Setup the validator to return true (valid code)
        _mockTotpValidator.Setup(v => v.ValidateCode("JBSWY3DPEHPK3PXP", "123456"))
            .Returns(true);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync("test@example.com", "111111");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<bool>>(result);
        Assert.False(successResult.Value);
    }

    #endregion
}
