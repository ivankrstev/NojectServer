using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations;
using NojectServer.Models;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Email;

public class EmailService(IOptions<EmailSettings> options) : IEmailService
{
    private readonly EmailSettings _emailSettings = options.Value;

    public async Task SendVerificationLinkAsync(User user)
    {
        if (user.VerificationToken == null) throw new Exception("Verification token is null");

        var verificationLink = BuildLink("verify-email", ("email", user.Email), ("token", user.VerificationToken));
        await SendEmail(
            user,
            "Email Verification",
            "verify your email",
            verificationLink
        );
    }

    public async Task SendResetPasswordLinkAsync(User user)
    {
        if (user.PasswordResetToken == null) throw new Exception("Password reset token is null");

        var resetLink = BuildLink("reset-password", ("token", user.PasswordResetToken));
        await SendEmail(
            user,
            "Password Reset Request",
            "reset your password",
            resetLink
        );
    }


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
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, _emailSettings.UseSsl);
        await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
        await smtp.SendAsync(emailMessage);
        await smtp.DisconnectAsync(true);
    }

    private string BuildLink(string path, params (string Key, string Value)[] parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{_emailSettings.ClientUrl}/{path}?{query}";
    }

    private static TextPart CreateTextPart(string fullName, string action, string link)
    {
        return new TextPart("plain")
        {
            Text = $"Dear {fullName},\nPlease click on the following link to {action}: {link}"
        };
    }

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