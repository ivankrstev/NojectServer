namespace NojectServer.Modules.Identity.Application.Passwords;

/// <summary>
/// Derives password hashes for persistence and verifies supplied passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Derives a password hash using a newly generated cryptographic salt.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The hash and salt to persist with the user.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="password"/> is <see langword="null"/>, empty,
    /// or consists only of white-space characters.
    /// </exception>
    HashedPassword Hash(string password);

    /// <summary>
    /// Verifies a plain-text password against a persisted hash and salt.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hash">The persisted password hash.</param>
    /// <param name="salt">The persisted password salt.</param>
    /// <returns>
    /// <see langword="true"/> when the password produces the persisted hash;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool Verify(string password, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> salt);
}
