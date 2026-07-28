namespace NojectServer.Modules.Identity.Application.Tokens;

/// <summary>
/// Generates cryptographically secure opaque tokens and hashes them for storage.
/// </summary>
public interface IOpaqueTokenGenerator
{
    /// <summary>
    /// Generates a URL-safe token with the requested amount of random data.
    /// </summary>
    /// <param name="sizeInBytes">The number of random bytes used to create the token.</param>
    /// <returns>The plain-text token and its hash.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sizeInBytes"/> is below the supported minimum.
    /// </exception>
    GeneratedToken Generate(int sizeInBytes);

    /// <summary>
    /// Computes the deterministic hash used to validate an opaque token.
    /// </summary>
    /// <param name="token">The plain-text token to hash.</param>
    /// <returns>The token hash.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="token"/> is empty or consists only of whitespace.
    /// </exception>
    byte[] ComputeHash(string token);
}
