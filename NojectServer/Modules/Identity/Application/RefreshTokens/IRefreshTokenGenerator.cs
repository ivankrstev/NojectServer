namespace NojectServer.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// Defines operations for creating refresh tokens and deriving their persisted hashes.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Computes a deterministic cryptographic hash for a refresh token value.
    /// </summary>
    /// <param name="token">The raw refresh token value to hash.</param>
    /// <returns>The hash bytes produced from the supplied token.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="token"/> is <see langword="null"/>, empty, or consists only of white-space characters.
    /// </exception>
    byte[] ComputeHash(string token);

    /// <summary>
    /// Creates a new refresh token value and its corresponding metadata.
    /// </summary>
    /// <returns>
    /// A generated refresh token containing the raw token value and hash used for persistence.
    /// </returns>
    GeneratedRefreshToken Generate();
}
