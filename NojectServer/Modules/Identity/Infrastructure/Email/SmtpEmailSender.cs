using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations.Email;
using NojectServer.Modules.Identity.Application.Email;

namespace NojectServer.Modules.Identity.Infrastructure.Email;

/// <summary>
/// Delivers email messages through an SMTP server.
/// </summary>
/// <param name="options">
/// The SMTP host, port, credentials, and transport-security configuration.
/// </param>
/// <remarks>
/// A new SMTP connection is opened for each delivery and is disconnected in a
/// <see langword="finally"/> block when the operation completes or fails.
/// </remarks>
internal sealed class SmtpEmailSender(
    IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendAsync(
        MimeMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var client = new SmtpClient();

        SecureSocketOptions socketOptions = _options.UseSsl is true
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            socketOptions,
            cancellationToken);

        try
        {
            await client.AuthenticateAsync(
                _options.UserName,
                _options.Password,
                cancellationToken);
            await client.SendAsync(
                message,
                cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    quit: true,
                    CancellationToken.None);
            }
        }
    }
}
