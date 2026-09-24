using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations.Email;
using NojectServer.Modules.Identity.Application.Email;

namespace NojectServer.Modules.Identity.Infrastructure.Email;

/// <summary>
/// Composes and sends identity-related emails.
/// </summary>
/// <param name="options">
/// The email configuration used for the sender identity and client application URL.
/// </param>
/// <param name="emailSender">
/// The transport used to deliver composed messages.
/// </param>
/// <remarks>
/// The service creates plain-text and HTML alternatives for each message. It accepts
/// raw tokens from the workflow that generated them because the persisted
/// <c>User</c> entity stores only token hashes.
/// </remarks>
public sealed class EmailService(
    IOptions<EmailOptions> options,
    IEmailSender emailSender) : IEmailService
{
    private readonly EmailOptions _options = options.Value;
    private readonly IEmailSender _emailSender = emailSender;

    /// <inheritdoc />
    public Task SendVerificationLinkAsync(
        string email,
        string fullName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        ValidateRecipient(email, fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationToken);

        string verificationLink = BuildLink(
            "verify-email",
            ("email", email),
            ("token", verificationToken));

        return SendEmailAsync(
            email,
            fullName,
            subject: "Email Verification",
            action: "verify your email",
            verificationLink,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task SendResetPasswordLinkAsync(
        string email,
        string fullName,
        string passwordResetToken,
        CancellationToken cancellationToken = default)
    {
        ValidateRecipient(email, fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordResetToken);

        string passwordResetLink = BuildLink(
            "reset-password",
            ("email", email),
            ("token", passwordResetToken));

        return SendEmailAsync(
            email,
            fullName,
            subject: "Password Reset Request",
            action: "reset your password",
            passwordResetLink,
            cancellationToken);
    }

    /// <summary>
    /// Creates a multipart email and passes it to the configured transport.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="fullName">The recipient's display name.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="action">The action described by the message.</param>
    /// <param name="link">The absolute URL for the action.</param>
    /// <param name="cancellationToken">A token used to cancel delivery.</param>
    /// <returns>A task representing the asynchronous delivery operation.</returns>
    private Task SendEmailAsync(
        string email,
        string fullName,
        string subject,
        string action,
        string link,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage
        {
            Subject = subject,
            Body = new MultipartAlternative
            {
                CreateTextPart(fullName, action, link),
                CreateHtmlPart(fullName, action, link)
            }
        };

        message.From.Add(
            new MailboxAddress(_options.Name, _options.EmailId));
        message.To.Add(
            new MailboxAddress(fullName, email));

        return _emailSender.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Builds an absolute client URL with URL-encoded query parameters.
    /// </summary>
    /// <param name="path">The client-relative path.</param>
    /// <param name="parameters">The query-string keys and values.</param>
    /// <returns>The absolute action URL.</returns>
    private string BuildLink(
        string path,
        params (string Key, string Value)[] parameters)
    {
        string query = string.Join(
            '&',
            parameters.Select(
                static parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        return $"{_options.ClientUrl.TrimEnd('/')}/{path}?{query}";
    }

    /// <summary>
    /// Creates the plain-text alternative of an identity email.
    /// </summary>
    /// <param name="fullName">The recipient's display name.</param>
    /// <param name="action">The action described by the message.</param>
    /// <param name="link">The absolute URL for the action.</param>
    /// <returns>A plain-text MIME part.</returns>
    private static TextPart CreateTextPart(
        string fullName,
        string action,
        string link)
    {
        return new TextPart("plain")
        {
            Text =
                $"Dear {fullName},{Environment.NewLine}{Environment.NewLine}" +
                $"Please use the following link to {action}:{Environment.NewLine}{link}"
        };
    }

    /// <summary>
    /// Creates the HTML alternative of an identity email.
    /// </summary>
    /// <param name="fullName">The recipient's display name.</param>
    /// <param name="action">The action described by the message.</param>
    /// <param name="link">The absolute URL for the action.</param>
    /// <returns>An HTML MIME part with user-controlled values encoded.</returns>
    private static TextPart CreateHtmlPart(
        string fullName,
        string action,
        string link)
    {
        string encodedFullName = HtmlEncoder.Default.Encode(fullName);
        string encodedAction = HtmlEncoder.Default.Encode(action);
        string encodedLink = HtmlEncoder.Default.Encode(link);

        return new TextPart("html")
        {
            Text = $"""
                <p>Dear {encodedFullName},</p>
                <p>Please use the following link to {encodedAction}:</p>
                <p><a href="{encodedLink}">{encodedAction}</a></p>
                """
        };
    }

    /// <summary>
    /// Ensures that the required recipient values are present.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="fullName">The recipient's display name.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a value is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a value is <see langword="null"/>.
    /// </exception>
    private static void ValidateRecipient(
        string email,
        string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
    }
}
