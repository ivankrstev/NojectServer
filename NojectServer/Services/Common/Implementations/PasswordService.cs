using NojectServer.Services.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace NojectServer.Services.Common.Implementations;

/// <summary>
/// Implementation of the IPasswordService interface that provides secure password hashing
/// and verification functionality.
///
/// This service uses HMACSHA512 cryptographic algorithm to generate password hashes and salts,
/// providing secure storage of user credentials. It handles both the creation of new password
/// hashes during user registration and verification of passwords during authentication.
/// </summary>
public class PasswordService : IPasswordService
{
    /// <summary>
    /// Creates a password hash and salt from a plain text password using HMACSHA512.
    /// </summary>
    /// <param name="password">The plain text password to hash. If null, an empty string is used.</param>
    /// <param name="passwordHash">The output parameter that will contain the generated password hash.</param>
    /// <param name="passwordSalt">The output parameter that will contain the generated salt used for hashing.</param>
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
    }

    /// <summary>
    /// Verifies if a plain text password matches a stored password hash using HMACSHA512.
    /// </summary>
    /// <param name="password">The plain text password to verify. If null, an empty string is used.</param>
    /// <param name="passwordHash">The stored password hash to compare against.</param>
    /// <param name="passwordSalt">The salt used when the hash was created.</param>
    /// <returns>True if the password matches the hash; otherwise, false. Returns false if passwordHash or passwordSalt is null.</returns>
    public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        // Check for null hash or salt
        if (passwordHash == null || passwordSalt == null)
            return false;
        using var hmac = new HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
        return computedHash.SequenceEqual(passwordHash);
    }
}
