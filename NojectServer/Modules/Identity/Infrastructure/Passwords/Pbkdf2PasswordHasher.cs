using System.Security.Cryptography;
using System.Text;
using NojectServer.Modules.Identity.Application.Passwords;

namespace NojectServer.Modules.Identity.Infrastructure.Passwords;

/// <summary>
/// Derives password hashes with PBKDF2-HMAC-SHA-512.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeInBytes = 32;
    private const int HashSizeInBytes = 32;
    private const int IterationCount = 210_000;

    /// <inheritdoc />
    public HashedPassword Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] hash = DeriveHash(password, salt);

        return new HashedPassword(hash, salt);
    }

    /// <inheritdoc />
    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentNullException.ThrowIfNull(salt);

        if (hash.Length != HashSizeInBytes || salt.Length != SaltSizeInBytes)
        {
            return false;
        }

        byte[] candidateHash = DeriveHash(password, salt);

        return CryptographicOperations.FixedTimeEquals(candidateHash, hash);
    }

    // Derives a password hash using PBKDF2-HMAC-SHA-512.
    private static byte[] DeriveHash(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(password),
            salt: salt,
            iterations: IterationCount,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: HashSizeInBytes);
    }
}
