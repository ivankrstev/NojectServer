namespace NojectServer.Configurations.Totp;

/// <summary>
/// Configuration options for time-based one-time passwords.
/// </summary>
public sealed class TotpOptions
{
    /// <summary>
    /// Configuration section name for TOTP settings.
    /// </summary>
    public const string SectionName = "Totp";

    /// <summary>
    /// Provider name displayed by authenticator applications.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;
}
