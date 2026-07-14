namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Configuration options for two-factor authentication tokens.
/// </summary>
public sealed class TfaTokenOptions
{
    /// <summary>
    /// Configuration section path for two-factor authentication token settings.
    /// </summary>
    public const string SectionName = $"{JwtOptions.SectionName}:Tfa";

    /// <summary>
    /// Secret key used to sign two-factor authentication tokens.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Two-factor authentication token lifetime in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; }
}
