using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations;
using NojectServer.Services.Email.Interfaces;

namespace NojectServer.Services.Email.Implementations;

public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings _emailSettings = options.Value;

    public async Task SendAsync(MimeMessage message)
    {
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, _emailSettings.UseSsl);
        await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
