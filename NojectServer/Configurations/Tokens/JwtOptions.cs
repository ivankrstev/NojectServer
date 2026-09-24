namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Configuration options for JWT issuer and audience values.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Configuration section name for JWT settings.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Token issuer value.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Token audience value.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Allowed clock skew, in seconds, when validating JWT timestamps.
    /// </summary>
    public int? ClockSkewInSeconds { get; init; }
}
