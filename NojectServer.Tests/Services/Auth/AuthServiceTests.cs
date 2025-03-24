using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Common.Interfaces;
using NojectServer.Services.Email.Interfaces;
using NojectServer.Tests.MockHelpers;
using NojectServer.Utils.ResultPattern;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Auth;

public class AuthServiceTests
{
    private readonly Mock<DataContext> _mockDataContext;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly List<User> _users;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Initialize test data
        _users = [];

        // Set up mocks
        _mockDataContext = new Mock<DataContext>(new DbContextOptions<DataContext>());
        _mockPasswordService = new Mock<IPasswordService>();
        _mockEmailService = new Mock<IEmailService>();

        // Set up the mock DbSet with our helper class
        var mockUsersDbSet = DbSetMockHelper.MockDbSet(_users);
        _mockDataContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        // Mock Database and Transaction
        var mockDatabase = new Mock<DatabaseFacade>(_mockDataContext.Object);
        _mockDataContext.Setup(c => c.Database).Returns(mockDatabase.Object);

        var mockTransaction = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);

        _authService = new AuthService(_mockDataContext.Object, _mockPasswordService.Object, _mockEmailService.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_UserExists_ReturnsConflictResult()
    {
        // Arrange
        var request = new UserRegisterRequest
        {
            Email = "existing@example.com",
            FullName = "Existing User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        // Add existing user to the collection
        _users.Add(new User { Email = "existing@example.com" });

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal("Conflict", failureResult.Error.Error);
        Assert.Equal("A user with the provided email already exists.", failureResult.Error.Message);
        Assert.Equal(409, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task RegisterAsync_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new UserRegisterRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Pass",  // Too short, will fail validation
            ConfirmPassword = "Password123" // Doesn't match password
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new UserRegisterRequest
        {
            Email = "new@example.com",
            FullName = "New User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        byte[] passwordHash = [1, 2, 3];
        byte[] passwordSalt = [4, 5, 6];

        _mockPasswordService
            .Setup(s => s.CreatePasswordHash(request.Password, out passwordHash, out passwordSalt));

        _mockDataContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailService.Setup(m => m.SendVerificationLinkAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<User>>(result);
        Assert.NotNull(successResult.Value);
        Assert.Equal(request.Email, successResult.Value.Email);
        Assert.Equal(request.FullName, successResult.Value.FullName);
    }

    [Fact]
    public async Task RegisterAsync_ExceptionThrown_ReturnsFailureResult()
    {
        // Arrange
        var request = new UserRegisterRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        byte[] passwordHash = [1, 2, 3];
        byte[] passwordSalt = [4, 5, 6];

        _mockPasswordService
            .Setup(s => s.CreatePasswordHash(request.Password, out passwordHash, out passwordSalt));

        var mockTransaction = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _mockDataContext.Setup(c => c.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);

        _mockDataContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal("ServerError", failureResult.Error.Error);
        Assert.Equal(500, failureResult.Error.StatusCode);
        mockTransaction.Verify(m => m.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "notfound@example.com",
            Password = "Password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal("Invalid credentials.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        // Add user to test data
        _users.Add(new User
        {
            Email = "user@example.com",
            Password = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            VerifiedAt = DateTime.UtcNow
        });

        _mockPasswordService
            .Setup(s => s.VerifyPasswordHash(request.Password, It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(false);

        //_mockPasswordService.Setup(s => s.VerifyPasswordHash(
        //    request.Password, user.Password, user.PasswordSalt))
        //    .Returns(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal("Invalid credentials.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_EmailNotVerified_ReturnsUnauthorized()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "unverified@example.com",
            Password = "Password123"
        };

        // Add unverified user to test data
        _users.Add(new User
        {
            Email = "unverified@example.com",
            Password = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            VerifiedAt = null
        });

        _mockPasswordService
            .Setup(s => s.VerifyPasswordHash(request.Password, It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(true);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal("Email not verified.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            Email = "verified@example.com",
            Password = "Password123"
        };

        // Add verified user to test data
        _users.Add(new User
        {
            Email = "verified@example.com",
            Password = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            VerifiedAt = DateTime.UtcNow
        });

        _mockPasswordService
            .Setup(s => s.VerifyPasswordHash(request.Password, It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(true);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal("verified@example.com", successResult.Value);
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsNotFound()
    {
        // Arrange
        var email = "user@example.com";
        var token = "invalid_token";

        // Act
        var result = await _authService.VerifyEmailAsync(email, token);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Contains("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task VerifyEmailAsync_AlreadyVerified_ReturnsConflict()
    {
        // Arrange
        var email = "verified@example.com";
        var token = "valid_token";

        // Add already verified user
        _users.Add(new User
        {
            Email = email,
            VerificationToken = token,
            VerifiedAt = DateTime.UtcNow
        });

        // Act
        var result = await _authService.VerifyEmailAsync(email, token);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Contains("Conflict", failureResult.Error.Error);
        Assert.Equal(409, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var email = "unverified@example.com";
        var token = "valid_token";

        // Add unverified user
        _users.Add(new User
        {
            Email = email,
            VerificationToken = token,
            VerifiedAt = null
        });

        _mockDataContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _authService.VerifyEmailAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Contains("successfully verified", successResult.Value.ToLowerInvariant());
    }

    #endregion

    #region ForgotPasswordAsync Tests

    [Fact]
    public async Task ForgotPasswordAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Contains("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var email = "user@example.com";

        // Add user to test data
        _users.Add(new User
        {
            Email = email
        });

        _mockDataContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailService.Setup(m => m.SendResetPasswordLinkAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Contains("Reset link", successResult.Value);
    }

    #endregion
}
