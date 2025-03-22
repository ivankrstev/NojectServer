using MimeKit;

namespace NojectServer.Services.Email.Interfaces;

public interface IEmailSender
{
    Task SendAsync(MimeMessage message);
}
