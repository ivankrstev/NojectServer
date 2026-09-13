namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Configuration options for restricted pending-email-verification tokens.
/// </summary>
public sealed class PendingEmailVerificationTokenOptions
{
    /// <summary>
    /// Configuration section path for pending-email-verification tokens.
    /// </summary>
    public const string SectionName =
        $"{JwtOptions.SectionName}:PendingEmailVerification";

    /// <summary>
    /// Secret key used only to sign pending-email-verification tokens.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Restricted token lifetime in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; }
}
