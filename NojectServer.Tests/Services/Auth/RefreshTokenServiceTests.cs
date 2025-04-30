using Moq;
using NojectServer.Models;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Utils.ResultPattern;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Auth;

public class RefreshTokenServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly List<RefreshToken> _refreshTokenList;
    private readonly Guid _testUserId = Guid.NewGuid();

    public RefreshTokenServiceTests()
    {
        // Set up test data
        _refreshTokenList = [];

        // Set up mock repositories
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();

        // Set up mock UnitOfWork
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(uow => uow.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Set up mock TokenService
        _mockTokenService = new Mock<ITokenService>();
        _mockTokenService.Setup(ts => ts.CreateRefreshToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns((Guid userId, string email) => $"mock-refresh-token-{userId}-{email}");

        // Create the service to test
        _refreshTokenService = new RefreshTokenService(_mockUnitOfWork.Object, _mockTokenService.Object);
    }

    #region GenerateRefreshToken Tests

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCreateAndReturnTokenAsync()
    {
        // Arrange
        string email = "test123@example.com";
        string expectedToken = $"mock-refresh-token-{_testUserId}-{email}";

        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => _refreshTokenList.Add(token))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync(_testUserId, email);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal(expectedToken, successResult.Value);
        _mockTokenService.Verify(ts => ts.CreateRefreshToken(_testUserId, email), Times.Once);
        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);

        // Verify token was added with correct properties
        var addedToken = _refreshTokenList.FirstOrDefault();
        Assert.NotNull(addedToken);
        Assert.Equal(_testUserId, addedToken.UserId);
        Assert.Equal(expectedToken, addedToken.Token);
        Assert.True(addedToken.ExpireDate > DateTime.UtcNow.AddDays(13) &&
                    addedToken.ExpireDate <= DateTime.UtcNow.AddDays(14));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithNullUserId_ShouldThrowArgumentNullExceptionAsync()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("userId",
            async () => await _refreshTokenService.GenerateRefreshTokenAsync(Guid.Empty, "test@example.com"));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithNullEmail_ShouldThrowArgumentNullExceptionAsync()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("email",
            async () => await _refreshTokenService.GenerateRefreshTokenAsync(_testUserId, null!));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WhenExceptionOccurs_ShouldReturnFailureResultAsync()
    {
        // Arrange
        string email = "test@example.com";
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync(_testUserId, email);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("TokenGenerationFailed", failureResult.Error.Error);
        Assert.Contains("Failed to generate refresh token: Database error", failureResult.Error.Message);
        Assert.Equal(500, failureResult.Error.StatusCode);
    }

    #endregion

    #region ValidateRefreshToken Tests

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnRefreshTokenAsync()
    {
        // Arrange
        var validToken = new RefreshToken
        {
            UserId = _testUserId,
            Token = "valid-token",
            ExpireDate = DateTime.UtcNow.AddDays(7) // Not expired
        };
        _refreshTokenList.Add(validToken);

        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("valid-token"))
            .ReturnsAsync(validToken);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<RefreshToken>>(result);
        Assert.Equal(validToken.UserId, successResult.Value.UserId);
        Assert.Equal(validToken.Token, successResult.Value.Token);
        Assert.Equal(validToken.ExpireDate, successResult.Value.ExpireDate);
        _mockRefreshTokenRepository.Verify(r => r.GetByTokenAsync("valid-token"), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ShouldReturnFailureResultAsync()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            UserId = _testUserId,
            Token = "expired-token",
            ExpireDate = DateTime.UtcNow.AddDays(-1) // Expired
        };
        _refreshTokenList.Add(expiredToken);

        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("expired-token"))
            .ReturnsAsync(expiredToken);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("expired-token");

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<RefreshToken>>(result);
        Assert.Equal("ExpiredToken", failureResult.Error.Error);
        Assert.Equal("Refresh token has expired.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
        _mockRefreshTokenRepository.Verify(r => r.GetByTokenAsync("expired-token"), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithInvalidToken_ShouldReturnFailureResultAsync()
    {
        // Arrange
        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("invalid-token"))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("invalid-token");

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<RefreshToken>>(result);
        Assert.Equal("InvalidToken", failureResult.Error.Error);
        Assert.Equal("Invalid refresh token.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
        _mockRefreshTokenRepository.Verify(r => r.GetByTokenAsync("invalid-token"), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithNullToken_ShouldThrowArgumentNullExceptionAsync()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("token",
            async () => await _refreshTokenService.ValidateRefreshTokenAsync(null!));
    }

    #endregion

    #region RevokeRefreshToken Tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRemoveTokenAsync()
    {
        // Arrange
        var token = new RefreshToken
        {
            UserId = _testUserId,
            Token = "valid-token",
            ExpireDate = DateTime.UtcNow.AddDays(7)
        };
        _refreshTokenList.Add(token);

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync("valid-token");

        // Assert
        Assert.True(result.IsSuccess);
        _mockRefreshTokenRepository.Verify(r => r.GetByTokenAsync("valid-token"), Times.Once);
        _mockRefreshTokenRepository.Verify(r => r.Remove(token), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        Assert.Empty(_refreshTokenList); // Verify token was removed
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldCompleteSuccessfullyAsync()
    {
        // Arrange
        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("invalid-token"))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync("invalid-token");

        // Assert
        Assert.True(result.IsSuccess);
        _mockRefreshTokenRepository.Verify(r => r.GetByTokenAsync("invalid-token"), Times.Once);
        _mockRefreshTokenRepository.Verify(r => r.Remove(It.IsAny<RefreshToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNullToken_ShouldThrowArgumentNullExceptionAsync()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("token",
            async () => await _refreshTokenService.RevokeRefreshTokenAsync(null!));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenExceptionOccurs_ShouldReturnFailureResultAsync()
    {
        // Arrange
        var token = new RefreshToken
        {
            UserId = _testUserId,
            Token = "valid-token",
            ExpireDate = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("valid-token"))
            .ReturnsAsync(token);
        _mockRefreshTokenRepository.Setup(r => r.Remove(token))
            .Throws(new Exception("Database error"));

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync("valid-token");

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<bool>>(result);
        Assert.Equal("RevocationFailed", failureResult.Error.Error);
        Assert.Contains("Failed to revoke refresh token: Database error", failureResult.Error.Message);
        Assert.Equal(500, failureResult.Error.StatusCode);
    }

    #endregion
}
