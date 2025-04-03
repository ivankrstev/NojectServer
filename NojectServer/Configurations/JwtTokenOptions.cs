namespace NojectServer.Configurations;

/// <summary>
/// Configuration settings for JSON Web Token (JWT) authentication in the application.
///
/// This class defines the properties required for JWT token generation and validation,
/// including issuer, audience, and credentials for different token types (access, refresh, and
/// two-factor authentication tokens). The values for these settings are loaded from the
/// "Jwt" section in the application configuration during startup.
/// </summary>
public class JwtTokenOptions
{
    /// <summary>
    /// The issuer (iss) claim for the JWT token, identifying who created and signed the token
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// The audience (aud) claim for the JWT token, identifying the recipients the token is intended for
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// Configuration for access tokens used for general API authentication
    /// </summary>
    public required JwtSigningCredentials Access { get; set; }

    /// <summary>
    /// Configuration for refresh tokens used to obtain new access tokens
    /// </summary>
    public required JwtSigningCredentials Refresh { get; set; }

    /// <summary>
    /// Configuration for two-factor authentication tokens
    /// </summary>
    public required JwtSigningCredentials Tfa { get; set; }

    /// <summary>
    /// Represents signing credentials for a specific JWT token type
    /// </summary>
    public class JwtSigningCredentials
    {
        /// <summary>
        /// Secret key used to sign the JWT token
        /// </summary>
        public required string SecretKey { get; set; }

        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        public required int ExpirationInSeconds { get; set; }
    }
}
