namespace NojectServer.Configurations;

/// <summary>
/// Configuration settings for email services in the application.
/// 
/// This class defines SMTP server connection properties and email sending parameters 
/// used by the EmailService to send verification emails, password reset links, and 
/// other system notifications. The values for these settings are loaded from the 
/// "EmailSettings" section in the application configuration during startup using:
/// </summary>
public class EmailSettings
{
    // The email address from which emails are sent
    public required string EmailId { get; set; }

    // The display name of the sender
    public required string Name { get; set; }

    // The SMTP server host name
    public required string Host { get; set; }

    // The SMTP server username
    public required string UserName { get; set; }

    // The SMTP server password
    public required string Password { get; set; }

    // The SMTP server port number
    public required int Port { get; set; }

    // The flag to enable SSL for the SMTP connection
    public required bool UseSsl { get; set; }

    // The client application URL, used for constructing email links
    public required string ClientUrl { get; set; }
}