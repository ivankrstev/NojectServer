using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Application.Users;

/// <summary>
/// Provides persistence operations for users required by identity use cases.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by id.
    /// </summary>
    Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a user exists for the supplied email address.
    /// </summary>
    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by email address.
    /// </summary>
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by the stored password reset token hash.
    /// </summary>
    Task<User?> GetByPasswordResetTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins tracking a new user for persistence.
    /// </summary>
    void Add(User user);

    /// <summary>
    /// Deletes the user only when the account is still unverified.
    /// </summary>
    /// <returns><see langword="true"/> when a row was deleted.</returns>
    Task<bool> DeleteUnverifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending user changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
