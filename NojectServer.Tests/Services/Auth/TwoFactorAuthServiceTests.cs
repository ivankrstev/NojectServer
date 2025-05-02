using Microsoft.Extensions.Configuration;
using Moq;
using NojectServer.Models;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Auth.Validation.Interfaces;
using NojectServer.Utils.ResultPattern;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Auth;

public class TwoFactorAuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITotpValidator> _mockTotpValidator;
    private readonly TwoFactorAuthService _service;

    public TwoFactorAuthServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTotpValidator = new Mock<ITotpValidator>();

        _service = new TwoFactorAuthService(
            _mockConfig.Object,
            _mockUnitOfWork.Object,
            _mockTotpValidator.Object);

        // Configure default configuration values
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(x => x.Value).Returns("Noject");
        _mockConfig.Setup(x => x["AppName"]).Returns("Noject");
    }

    #region DisableTwoFactorAsync Tests

    [Fact]
    public async Task DisableTwoFactorAsync_UserNotFound_ReturnsNotFoundFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.DisableTwoFactorAsync(userId, "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_TwoFactorAlreadyDisabled_ReturnsBadRequestFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorEnabled = false
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.DisableTwoFactorAsync(userId, "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_InvalidCode_ReturnsUnauthorizedFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorEnabled = true,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP" // This is a valid Base32 string for testing
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockTotpValidator.Setup(v => v.ValidateCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _service.DisableTwoFactorAsync(userId, "111111"); // Invalid code

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_ValidCode_DisablesTwoFactorAndReturnsSuccessAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorEnabled = true,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP"
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockTotpValidator.Setup(v => v.ValidateCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.DisableTwoFactorAsync(userId, "123456");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal("2FA disabled successfully.", successResult.Value);

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorSecretKey);
        _mockUnitOfWork.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region EnableTwoFactorAsync Tests

    [Fact]
    public async Task EnableTwoFactorAsync_UserNotFound_ReturnsNotFoundFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.EnableTwoFactorAsync(userId, "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_NoSecretKey_ReturnsBadRequestFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorSecretKey = null
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.EnableTwoFactorAsync(userId, "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_InvalidCode_ReturnsUnauthorizedFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP",
            TwoFactorEnabled = false
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockTotpValidator.Setup(v => v.ValidateCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _service.EnableTwoFactorAsync(userId, "111111"); // Invalid code

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_ValidCode_EnablesTwoFactorAndReturnsSuccessAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP",
            TwoFactorEnabled = false
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockTotpValidator.Setup(v => v.ValidateCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.EnableTwoFactorAsync(userId, "123456");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal("2FA enabled successfully.", successResult.Value);

        Assert.True(user.TwoFactorEnabled);
        Assert.NotNull(user.TwoFactorSecretKey);
        _mockUnitOfWork.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GenerateSetupCodeAsync Tests

    [Fact]
    public async Task GenerateSetupCodeAsync_UserNotFound_ReturnsNotFoundFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GenerateSetupCodeAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<TwoFactorSetupResult>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_TwoFactorAlreadyEnabled_ReturnsBadRequestFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorEnabled = true
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GenerateSetupCodeAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<TwoFactorSetupResult>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_Success_GeneratesCodeAndReturnsSetupDataAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            TwoFactorEnabled = false
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GenerateSetupCodeAsync(userId);

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
        _mockUnitOfWork.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region ValidateTwoFactorCodeAsync Tests

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_UserNotFound_ReturnsNotFoundFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync(userId, "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<bool>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_NoSecretKey_ReturnsBadRequestFailureAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorSecretKey = null
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync(userId, "123456");

        // Assert
        Assert.False(result.IsSuccess);

        // Cast to access Error property
        var failureResult = Assert.IsType<FailureResult<bool>>(result);
        Assert.Equal("BadRequest", failureResult.Error.Error);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_WithValidCode_ReturnsTrueAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP"
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Setup the validator to return true (valid code)
        _mockTotpValidator.Setup(v => v.ValidateCode("JBSWY3DPEHPK3PXP", "123456"))
            .Returns(true);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync(userId, "123456");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<bool>>(result);
        Assert.True(successResult.Value);
    }

    [Fact]
    public async Task ValidateTwoFactorCodeAsync_WithInvalidCode_ReturnsFalseAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TwoFactorSecretKey = "JBSWY3DPEHPK3PXP"
        };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Setup the validator to return false (invalid code)
        _mockTotpValidator.Setup(v => v.ValidateCode("JBSWY3DPEHPK3PXP", "111111"))
            .Returns(false);

        // Act
        var result = await _service.ValidateTwoFactorCodeAsync(userId, "111111");

        // Assert
        Assert.True(result.IsSuccess);

        // Cast to access Value property
        var successResult = Assert.IsType<SuccessResult<bool>>(result);
        Assert.False(successResult.Value);
    }

    #endregion
}
