namespace NojectServer.Configurations.Email;

/// <summary>
/// Configuration settings for email services in the application.
///
/// This class defines SMTP server connection properties and email sending parameters
/// used by the EmailService to send verification emails, password reset links, and
/// other system notifications. The values for these settings are loaded from the
/// "EmailSettings" section in the application configuration during startup.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// The email address from which messages are sent.
    /// </summary>
    public string EmailId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the sender.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The SMTP server host name.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// The SMTP server username.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// The SMTP server password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The SMTP server port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Indicates whether SSL/TLS should be enabled for the SMTP connection.
    /// </summary>
    public bool? UseSsl { get; set; }

    /// <summary>
    /// The client application's base URL used to construct email links.
    /// </summary>
    public string ClientUrl { get; set; } = string.Empty;
}
