namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Configuration options for access token generation.
/// </summary>
public sealed class AccessTokenOptions
{
    /// <summary>
    /// Configuration section path for access token settings.
    /// </summary>
    public const string SectionName = $"{JwtOptions.SectionName}:Access";

    /// <summary>
    /// Secret key used to sign access tokens.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; }
}
