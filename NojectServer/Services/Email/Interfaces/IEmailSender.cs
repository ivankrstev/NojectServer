using MimeKit;

namespace NojectServer.Services.Email.Interfaces;

/// <summary>
/// Service for sending email messages through a specific delivery method
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message
    /// </summary>
    /// <param name="message">The email message to be sent</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    Task SendAsync(MimeMessage message);
}
