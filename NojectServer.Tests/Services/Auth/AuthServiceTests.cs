using Moq;
using NojectServer.Models;
using NojectServer.Models.Requests.Auth;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Common.Interfaces;
using NojectServer.Services.Email.Interfaces;
using NojectServer.Utils.ResultPattern;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Set up mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockEmailService = new Mock<IEmailService>();

        // Initialize the service with mocked dependencies
        _authService = new AuthService(_mockUnitOfWork.Object, _mockPasswordService.Object, _mockEmailService.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_UserExists_ReturnsConflictResultAsync()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            FullName = "Existing User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        // Setup mock to return true for user existence check
        _mockUnitOfWork.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(true);

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
    public async Task RegisterAsync_InvalidRequest_ReturnsBadRequestAsync()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Pass",  // Too short, will fail validation
            ConfirmPassword = "Password123" // Doesn't match password
        };

        // Setup mock to return false for user existence check
        _mockUnitOfWork.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal(400, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessAsync()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new@example.com",
            FullName = "New User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        byte[] passwordHash = [1, 2, 3];
        byte[] passwordSalt = [4, 5, 6];

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        _mockPasswordService
            .Setup(s => s.CreatePasswordHash(request.Password, out passwordHash, out passwordSalt));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.Users.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _mockEmailService.Setup(m => m.SendVerificationLinkAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<User>>(result);
        Assert.NotNull(successResult.Value);
        Assert.Equal(request.Email, successResult.Value.Email);
        Assert.Equal(request.FullName, successResult.Value.FullName);

        // Verify transaction operations were called
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.Users.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockEmailService.Verify(e => e.SendVerificationLinkAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExceptionThrown_ReturnsFailureResultAsync()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        byte[] passwordHash = [1, 2, 3];
        byte[] passwordSalt = [4, 5, 6];

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        _mockPasswordService
            .Setup(s => s.CreatePasswordHash(request.Password, out passwordHash, out passwordSalt));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal("ServerError", failureResult.Error.Error);
        Assert.Equal(500, failureResult.Error.StatusCode);

        // Verify rollback was called
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsUnauthorizedAsync()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "notfound@example.com",
            Password = "Password123"
        };

        // Setup mock to return null for user lookup
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(value: null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal("Invalid credentials.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorizedAsync()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        // Create user for the test
        var user = new User
        {
            Email = "user@example.com",
            Password = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            VerifiedAt = DateTime.UtcNow
        };

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockPasswordService
            .Setup(s => s.VerifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal("Invalid credentials.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_EmailNotVerified_ReturnsUnauthorizedAsync()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "unverified@example.com",
            Password = "Password123"
        };

        // Create unverified user
        var user = new User
        {
            Email = "unverified@example.com",
            Password = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            VerifiedAt = null
        };

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockPasswordService
            .Setup(s => s.VerifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
            .Returns(true);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<User>>(result);
        Assert.Equal("Unauthorized", failureResult.Error.Error);
        Assert.Equal("Email not verified.", failureResult.Error.Message);
        Assert.Equal(401, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessAsync()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "verified@example.com",
            Password = "Password123"
        };

        // Create verified user
        var user = new User
        {
            Email = "verified@example.com",
            Password = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            VerifiedAt = DateTime.UtcNow
        };

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockPasswordService
            .Setup(s => s.VerifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
            .Returns(true);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<User>>(result);
        Assert.Equal(user, successResult.Value);
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsNotFoundAsync()
    {
        // Arrange
        var email = "user@example.com";
        var token = "invalid_token";

        // Setup mock to return empty list
        _mockUnitOfWork.Setup(u => u.Users.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _authService.VerifyEmailAsync(email, token);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Contains("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task VerifyEmailAsync_AlreadyVerified_ReturnsConflictAsync()
    {
        // Arrange
        var email = "verified@example.com";
        var token = "valid_token";

        // Create already verified user
        var user = new User
        {
            Email = email,
            VerificationToken = token,
            VerifiedAt = DateTime.UtcNow
        };

        // Setup mock to return the user
        _mockUnitOfWork.Setup(u => u.Users.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);

        // Act
        var result = await _authService.VerifyEmailAsync(email, token);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Contains("Conflict", failureResult.Error.Error);
        Assert.Equal(409, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ReturnsSuccessAsync()
    {
        // Arrange
        var email = "unverified@example.com";
        var token = "valid_token";

        // Create unverified user
        var user = new User
        {
            Email = email,
            VerificationToken = token,
            VerifiedAt = null
        };

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.VerifyEmailAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Contains("successfully verified", successResult.Value.ToLowerInvariant());

        // Verify user was updated
        _mockUnitOfWork.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        Assert.NotNull(user.VerifiedAt);
    }

    #endregion

    #region ForgotPasswordAsync Tests

    [Fact]
    public async Task ForgotPasswordAsync_UserNotFound_ReturnsNotFoundAsync()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Setup mock to return null for user lookup
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(email))
            .ReturnsAsync(value: null);

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Contains("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ValidRequest_ReturnsSuccessAsync()
    {
        // Arrange
        var email = "user@example.com";

        // Create user for the test
        var user = new User
        {
            Email = email
        };

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _mockEmailService.Setup(m => m.SendResetPasswordLinkAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Contains("Reset link", successResult.Value);

        // Verify user was updated
        _mockUnitOfWork.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockEmailService.Verify(e => e.SendResetPasswordLinkAsync(It.IsAny<User>()), Times.Once);
        Assert.NotNull(user.PasswordResetToken);
        Assert.NotNull(user.ResetTokenExpires);
    }

    [Fact]
    public async Task ForgotPasswordAsync_EmailSendingFails_ReturnsFailureAsync()
    {
        // Arrange
        var email = "user@example.com";

        // Create user for the test
        var user = new User
        {
            Email = email
        };

        // Setup mocks
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _mockEmailService.Setup(m => m.SendResetPasswordLinkAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Email sending failed"));

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("ServerError", failureResult.Error.Error);
        Assert.Contains("Failed to send reset email", failureResult.Error.Message);
        Assert.Equal(500, failureResult.Error.StatusCode);
    }

    #endregion
}
