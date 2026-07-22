namespace NojectServer.Modules.Identity.Application.TwoFactorAuthentication;

/// <summary>
/// Provides the TOTP primitives used to enroll and authenticate users.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Creates a new cryptographically secure TOTP secret.
    /// </summary>
    /// <returns>The raw secret bytes.</returns>
    byte[] GenerateSecret();

    /// <summary>
    /// Encodes a raw TOTP secret as a Base32 manual setup key.
    /// </summary>
    /// <param name="secret">The raw TOTP secret bytes.</param>
    /// <returns>The Base32-encoded secret.</returns>
    string EncodeSecret(byte[] secret);

    /// <summary>
    /// Creates an authenticator-compatible provisioning URI for a TOTP secret.
    /// </summary>
    /// <param name="secret">The raw TOTP secret bytes.</param>
    /// <param name="accountName">The account label, typically the user's email address.</param>
    /// <returns>An <c>otpauth://</c> provisioning URI.</returns>
    string CreateProvisioningUri(byte[] secret, string accountName);

    /// <summary>
    /// Attempts to validate a TOTP code for the supplied secret using the current time.
    /// </summary>
    /// <param name="secret">The raw TOTP secret bytes.</param>
    /// <param name="code">The code supplied by the authenticator, or <see langword="null" />.</param>
    /// <param name="matchedTimeStep">
    /// When validation succeeds, receives the time step matched by the code.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the code is valid at the specified time;
    /// otherwise, <see langword="false" />.
    /// </returns>
    bool TryValidateCode(
        byte[] secret,
        string? code,
        out long matchedTimeStep);
}
