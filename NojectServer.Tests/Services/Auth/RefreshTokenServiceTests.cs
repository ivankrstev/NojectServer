using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Auth.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Auth;

public class RefreshTokenServiceTests
{
    private readonly Mock<DataContext> _mockDataContext;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<DbSet<RefreshToken>> _mockRefreshTokens;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly List<RefreshToken> _refreshTokenList;

    public RefreshTokenServiceTests()
    {
        // Set up test data
        _refreshTokenList = [];

        // Set up mock DbSet
        _mockRefreshTokens = DbSetMockHelper.MockDbSet(_refreshTokenList);

        // Set up mock DataContext
        _mockDataContext = new Mock<DataContext>(new DbContextOptions<DataContext>());
        _mockDataContext.Setup(c => c.RefreshTokens).Returns(_mockRefreshTokens.Object);
        _mockDataContext.Setup(c => c.SaveChangesAsync(default))
            .ReturnsAsync(1)
            .Callback(() => { /* Mock SaveChanges by not doing anything */ });

        // Set up mock TokenService
        _mockTokenService = new Mock<ITokenService>();
        _mockTokenService.Setup(ts => ts.CreateRefreshToken(It.IsAny<string>()))
            .Returns((string email) => $"mock-refresh-token-{email}");

        // Create the service to test
        _refreshTokenService = new RefreshTokenService(_mockDataContext.Object, _mockTokenService.Object);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCreateAndReturnToken()
    {
        // Arrange
        string email = "test123@example.com";
        string expectedToken = $"mock-refresh-token-{email}";

        // Act
        string result = await _refreshTokenService.GenerateRefreshTokenAsync(email);

        // Assert
        Assert.Equal(expectedToken, result);
        _mockTokenService.Verify(ts => ts.CreateRefreshToken(email), Times.Once);
        _mockDataContext.Verify(dc => dc.RefreshTokens.Add(It.IsAny<RefreshToken>()), Times.Once);
        _mockDataContext.Verify(dc => dc.SaveChangesAsync(default), Times.Once);

        // Verify token was added with correct properties
        var addedToken = _refreshTokenList.FirstOrDefault();
        Assert.NotNull(addedToken);
        Assert.Equal(email, addedToken.Email);
        Assert.Equal(expectedToken, addedToken.Token);
        Assert.True(addedToken.ExpireDate > DateTime.UtcNow.AddDays(13) &&
                    addedToken.ExpireDate <= DateTime.UtcNow.AddDays(14));
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnRefreshToken()
    {
        // Arrange
        var validToken = new RefreshToken
        {
            Email = "test@example.com",
            Token = "valid-token",
            ExpireDate = DateTime.UtcNow.AddDays(7) // Not expired
        };
        _refreshTokenList.Add(validToken);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validToken.Email, result.Email);
        Assert.Equal(validToken.Token, result.Token);
        Assert.Equal(validToken.ExpireDate, result.ExpireDate);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ShouldThrowSecurityTokenException()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            Email = "test@example.com",
            Token = "expired-token",
            ExpireDate = DateTime.UtcNow.AddDays(-1) // Expired
        };
        _refreshTokenList.Add(expiredToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            async () => await _refreshTokenService.ValidateRefreshTokenAsync("expired-token"));

        Assert.Contains("Invalid or expired", exception.Message);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithInvalidToken_ShouldThrowSecurityTokenException()
    {
        // Arrange - No tokens in the list matches "invalid-token"

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityTokenException>(
            async () => await _refreshTokenService.ValidateRefreshTokenAsync("invalid-token"));

        Assert.Contains("Invalid or expired", exception.Message);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRemoveToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Email = "test@example.com",
            Token = "valid-token",
            ExpireDate = DateTime.UtcNow.AddDays(7)
        };
        _refreshTokenList.Add(token);

        // Act
        await _refreshTokenService.RevokeRefreshTokenAsync("valid-token");

        // Assert
        _mockDataContext.Verify(dc => dc.RefreshTokens.Remove(It.IsAny<RefreshToken>()), Times.Once);
        _mockDataContext.Verify(dc => dc.SaveChangesAsync(default), Times.Once);
        Assert.Empty(_refreshTokenList); // Verify token was removed
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldNotThrowException()
    {
        // Arrange - No tokens in list

        // Act - Should not throw exception
        await _refreshTokenService.RevokeRefreshTokenAsync("invalid-toke");

        // Assert - Verify no removal happened
        _mockDataContext.Verify(dc => dc.RefreshTokens.Remove(It.IsAny<RefreshToken>()), Times.Never);
        _mockDataContext.Verify(dc => dc.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithNullEmail_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("email",
            async () => await _refreshTokenService.GenerateRefreshTokenAsync(null!));
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("token",
            async () => await _refreshTokenService.ValidateRefreshTokenAsync(null!));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("token",
            async () => await _refreshTokenService.RevokeRefreshTokenAsync(null!));
    }
}
