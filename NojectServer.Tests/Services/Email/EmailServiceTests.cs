using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using NojectServer.Configurations;
using NojectServer.Models;
using NojectServer.Services.Email.Implementations;
using NojectServer.Services.Email.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Email;

public class EmailServiceTests
{
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly EmailSettings _emailSettings;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        // Setup mock dependencies
        _mockEmailSender = new Mock<IEmailSender>();

        _emailSettings = new EmailSettings
        {
            EmailId = "test@example.com",
            Name = "Test Sender",
            Host = "smtp.example.com",
            UserName = "testuser",
            Password = "testpassword",
            Port = 587,
            UseSsl = true,
            ClientUrl = "https://example.com"
        };

        var mockOptions = new Mock<IOptions<EmailSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_emailSettings);

        _emailService = new EmailService(mockOptions.Object, _mockEmailSender.Object);
    }

    #region SendVerificationLinkAsync Tests

    /// <summary>
    /// Verifies that the email is sent when the user is valid.
    /// </summary>
    [Fact]
    public async Task SendVerificationLinkAsync_WithValidUser_SendsEmailAsync()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            FullName = "Test User",
            VerificationToken = "verification-token-123"
        };

        _mockEmailSender.Setup(s => s.SendAsync(It.IsAny<MimeMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _emailService.SendVerificationLinkAsync(user);

        // Assert
        _mockEmailSender.Verify(s => s.SendAsync(
            It.Is<MimeMessage>(m =>
                m.Subject == "Email Verification" &&
                m.From.Count == 1 &&
                m.From[0].ToString() == $"\"{_emailSettings.Name}\" <{_emailSettings.EmailId}>" &&
                m.To.Count == 1 &&
                m.To[0].ToString() == $"\"{user.FullName}\" <{user.Email}>")),
            Times.Once);
    }

    /// <summary>
    /// Verifies that an exception is thrown when the verification token is null.
    /// </summary>
    [Fact]
    public async Task SendVerificationLinkAsync_WithNullToken_ThrowsExceptionAsync()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            FullName = "Test User",
            VerificationToken = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _emailService.SendVerificationLinkAsync(user));

        Assert.Equal("Verification token is null", exception.Message);
        _mockEmailSender.Verify(s => s.SendAsync(It.IsAny<MimeMessage>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the correct verification link is constructed and included in the email body.
    /// </summary>
    [Fact]
    public async Task SendVerificationLinkAsync_VerifiesCorrectLinkConstructionAsync()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            FullName = "Test User",
            VerificationToken = "verification-token-123"
        };

        MimeMessage? capturedMessage = null;
        _mockEmailSender.Setup(s => s.SendAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(m => capturedMessage = m)
            .Returns(Task.CompletedTask);

        // Act
        await _emailService.SendVerificationLinkAsync(user);

        // Assert
        Assert.NotNull(capturedMessage);
        var body = capturedMessage.Body.ToString();

        // Check for the expected link format in the email body
        var expectedLinkPart = $"{_emailSettings.ClientUrl}/verify-email?email={Uri.EscapeDataString(user.Email)}&token={user.VerificationToken}";
        Assert.Contains(expectedLinkPart, body);
    }

    #endregion

    #region SendResetPasswordLinkAsync Tests

    /// <summary>
    /// Verifies that the email is sent when the user is valid.
    /// </summary>
    [Fact]
    public async Task SendResetPasswordLinkAsync_WithValidUser_SendsEmailAsync()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            FullName = "Test User",
            PasswordResetToken = "reset-token-456"
        };

        _mockEmailSender.Setup(s => s.SendAsync(It.IsAny<MimeMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _emailService.SendResetPasswordLinkAsync(user);

        // Assert
        _mockEmailSender.Verify(s => s.SendAsync(
            It.Is<MimeMessage>(m =>
                m.Subject == "Password Reset Request" &&
                m.From.Count == 1 &&
                m.From[0].ToString() == $"\"{_emailSettings.Name}\" <{_emailSettings.EmailId}>" &&
                m.To.Count == 1 &&
                m.To[0].ToString() == $"\"{user.FullName}\" <{user.Email}>")),
            Times.Once);
    }

    /// <summary>
    /// Verifies that an exception is thrown when the password reset token is null.
    /// </summary>
    [Fact]
    public async Task SendResetPasswordLinkAsync_WithNullToken_ThrowsExceptionAsync()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            FullName = "Test User",
            PasswordResetToken = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _emailService.SendResetPasswordLinkAsync(user));

        Assert.Equal("Password reset token is null", exception.Message);
        _mockEmailSender.Verify(s => s.SendAsync(It.IsAny<MimeMessage>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the correct reset password link is constructed and included in the email body.
    /// </summary>
    [Fact]
    public async Task SendResetPasswordLinkAsync_VerifiesCorrectLinkConstructionAsync()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            FullName = "Test User",
            PasswordResetToken = "reset-token-456"
        };

        MimeMessage? capturedMessage = null;
        _mockEmailSender.Setup(s => s.SendAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(m => capturedMessage = m)
            .Returns(Task.CompletedTask);

        // Act
        await _emailService.SendResetPasswordLinkAsync(user);

        // Assert
        Assert.NotNull(capturedMessage);
        var body = capturedMessage.Body.ToString();

        // Check for the expected link format in the email body
        var expectedLinkPart = $"{_emailSettings.ClientUrl}/reset-password?token={user.PasswordResetToken}";
        Assert.Contains(expectedLinkPart, body);
    }

    #endregion
}
