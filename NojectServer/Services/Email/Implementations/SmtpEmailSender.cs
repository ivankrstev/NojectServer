using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations;
using NojectServer.Services.Email.Interfaces;

namespace NojectServer.Services.Email.Implementations;

/// <summary>
/// Implementation of the IEmailSender interface that sends emails using SMTP protocol.
///
/// This class handles the low-level email delivery by establishing connections to
/// the configured SMTP server, authenticating with credentials from application settings,
/// and transmitting email messages. It encapsulates all SMTP-specific functionality
/// to provide a clean abstraction for sending emails in the application.
/// </summary>
public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings _emailSettings = options.Value;

    /// <summary>
    /// Sends an email message using SMTP protocol.
    /// </summary>
    /// <param name="message">The email message to be sent</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    /// <remarks>
    /// This method establishes a connection to the configured SMTP server,
    /// authenticates using the provided credentials, sends the email message,
    /// and properly disconnects from the server.
    /// </remarks>
    public async Task SendAsync(MimeMessage message)
    {
        using var smtp = new SmtpClient();
        SecureSocketOptions secureSocketOptions = _emailSettings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, secureSocketOptions);
        await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
