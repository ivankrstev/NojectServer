namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Configuration options for refresh token lifetime.
/// </summary>
public sealed class RefreshTokenOptions
{
    /// <summary>
    /// Configuration section name for refresh token settings.
    /// </summary>
    public const string SectionName = "RefreshToken";

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int ExpirationInDays { get; init; }
}
