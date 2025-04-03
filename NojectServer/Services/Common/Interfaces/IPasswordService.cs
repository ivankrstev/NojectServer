namespace NojectServer.Services.Common.Interfaces;

/// <summary>
/// Provides functionality for password hashing and verification.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Creates a password hash and salt from a plain text password.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <param name="passwordHash">The output parameter that will contain the generated password hash.</param>
    /// <param name="passwordSalt">The output parameter that will contain the generated salt used for hashing.</param>
    void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);

    /// <summary>
    /// Verifies if a plain text password matches a stored password hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="passwordHash">The stored password hash to compare against.</param>
    /// <param name="passwordSalt">The salt used when the hash was created.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
}
