namespace NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;

/// <summary>
/// Protects and restores two-factor authentication secrets for a specific user.
/// </summary>
public interface ITwoFactorSecretProtector
{
    /// <summary>
    /// Encrypts a two-factor authentication secret and binds it to the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the secret.</param>
    /// <param name="secret">The plaintext secret bytes to protect.</param>
    /// <returns>The protected secret bytes suitable for persistent storage.</returns>
    byte[] Protect(Guid userId, byte[] secret);

    /// <summary>
    /// Decrypts a protected two-factor authentication secret for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the secret.</param>
    /// <param name="protectedSecret">The protected secret bytes to restore.</param>
    /// <returns>The original plaintext secret bytes.</returns>
    byte[] Unprotect(Guid userId, byte[] protectedSecret);
}
