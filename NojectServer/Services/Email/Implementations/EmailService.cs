using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations;
using NojectServer.Models;
using NojectServer.Services.Email.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Email.Implementations;

/// <summary>
/// Implementation of the IEmailService interface that provides high-level email
/// functionality for the application.
///
/// This service is responsible for creating well-formatted emails with appropriate
/// content for various application functions such as account verification and password
/// reset. It handles the creation of both plain text and HTML versions of emails,
/// proper formatting of links, and delegating the actual sending to an IEmailSender
/// implementation.
/// </summary>
public class EmailService(
    IOptions<EmailSettings> options,
    IEmailSender emailSender) : IEmailService
{
    private readonly EmailSettings _emailSettings = options.Value;
    private readonly IEmailSender _emailSender = emailSender;

    /// <summary>
    /// Sends an email with a verification link to a user.
    /// </summary>
    /// <param name="user">The user to send the verification email to</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    /// <exception cref="Exception">Thrown when the user's verification token is null</exception>
    /// <remarks>
    /// This method builds a verification link with the user's email and verification token,
    /// then sends an email containing this link to the user.
    /// </remarks>
    public async Task SendVerificationLinkAsync(User user)
    {
        if (user.VerificationToken == null)
            throw new ArgumentException("User does not have a verification token", nameof(user));

        var verificationLink = BuildLink("verify-email", ("email", user.Email), ("token", user.VerificationToken));
        await SendEmail(user, "Email Verification", "verify your email", verificationLink);
    }

    /// <summary>
    /// Sends an email with a password reset link to a user.
    /// </summary>
    /// <param name="user">The user to send the password reset email to</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    /// <exception cref="Exception">Thrown when the user's password reset token is null</exception>
    /// <remarks>
    /// This method builds a password reset link with the user's password reset token,
    /// then sends an email containing this link to the user.
    /// </remarks>
    public async Task SendResetPasswordLinkAsync(User user)
    {
        if (user.PasswordResetToken == null)
            throw new ArgumentException("User does not have a password reset token", nameof(user));

        var resetLink = BuildLink("reset-password", ("token", user.PasswordResetToken));
        await SendEmail(user, "Password Reset Request", "reset your password", resetLink);
    }

    /// <summary>
    /// Sends an email to a user with specified subject, action, and link.
    /// </summary>
    /// <param name="user">The recipient user</param>
    /// <param name="subject">The email subject</param>
    /// <param name="action">The action description for the email content</param>
    /// <param name="link">The action link to include in the email</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    /// <remarks>
    /// This method creates an email message with both plain text and HTML versions
    /// of the content and sends it through the configured email sender.
    /// </remarks>
    private async Task SendEmail(User user, string subject, string action, string link)
    {
        var emailMessage = new MimeMessage();
        // Set sender and recipient
        var emailFrom = new MailboxAddress(_emailSettings.Name, _emailSettings.EmailId);
        emailMessage.From.Add(emailFrom);
        var emailTo = new MailboxAddress(user.FullName, user.Email);
        emailMessage.To.Add(emailTo);
        emailMessage.Subject = subject;

        // Create both plain text and HTML parts
        var textPart = CreateTextPart(user.FullName, action, link);
        var htmlPart = CreateHtmlPart(user.FullName, action, link);
        emailMessage.Body = new Multipart("alternative") { textPart, htmlPart };

        await _emailSender.SendAsync(emailMessage);
    }

    /// <summary>
    /// Builds a link URL with query parameters.
    /// </summary>
    /// <param name="path">The base path of the link</param>
    /// <param name="parameters">The query parameters to append to the URL</param>
    /// <returns>A fully formed URL with query parameters</returns>
    /// <remarks>
    /// This method constructs a URL by combining the client URL from settings,
    /// the specified path, and properly URL-encoded query parameters.
    /// </remarks>
    private string BuildLink(string path, params (string Key, string Value)[] parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{_emailSettings.ClientUrl}/{path}?{query}";
    }

    /// <summary>
    /// Creates a plain text version of the email content.
    /// </summary>
    /// <param name="fullName">The recipient's full name</param>
    /// <param name="action">The action description</param>
    /// <param name="link">The action link</param>
    /// <returns>A TextPart containing plain text email content</returns>
    private static TextPart CreateTextPart(string fullName, string action, string link)
    {
        return new TextPart("plain")
        {
            Text = $"Dear {fullName},\nPlease click on the following link to {action}: {link}"
        };
    }

    /// <summary>6
    /// Creates an HTML version of the email content.
    /// </summary>
    /// <param name="fullName">The recipient's full name</param>
    /// <param name="action">The action description</param>
    /// <param name="link">The action link</param>
    /// <returns>A TextPart containing HTML formatted email content</returns>
    private static TextPart CreateHtmlPart(string fullName, string action, string link)
    {
        return new TextPart("html")
        {
            Text = $"""
                <p style="font-size:16px;color:#333;margin-bottom:10px;">Dear {fullName},</p>
                                        <p style="font-size:14px;color:#555;margin-bottom:20px;">Please click on the following link to {action}:</p>
                                        <a style="display:inline-block;padding:10px 20px;background-color:#337ab7;color:#fff;text-decoration:none;border-radius:4px;font-size:14px;" href="{link}">{action}</a>
                """
        };
    }
}
