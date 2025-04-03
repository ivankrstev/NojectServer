namespace NojectServer.Configurations;

/// <summary>
/// Configuration settings for email services in the application.
///
/// This class defines SMTP server connection properties and email sending parameters
/// used by the EmailService to send verification emails, password reset links, and
/// other system notifications. The values for these settings are loaded from the
/// "EmailSettings" section in the application configuration during startup.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// The email address from which emails are sent
    /// </summary>
    public required string EmailId { get; set; }

    /// <summary>
    /// The display name of the sender
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The SMTP server host name
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// The SMTP server username
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// The SMTP server password
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// The SMTP server port number
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// The flag to enable SSL for the SMTP connection
    /// </summary>
    public required bool UseSsl { get; set; }

    /// <summary>
    /// The client application URL, used for constructing email links
    /// </summary>
    public required string ClientUrl { get; set; }
}
