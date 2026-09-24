using Microsoft.EntityFrameworkCore;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Application.Users.Exceptions;
using NojectServer.Modules.Identity.Domain;
using Npgsql;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserRepository(IdentityDataContext dbContext) : IUserRepository
{
    private const string NormalizedEmailUniqueIndexName = "ix_users_normalized_email";

    private readonly IdentityDataContext _dbContext = dbContext;

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(email);

        return _dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(email);

        return _dbContext.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByPasswordResetTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        if (tokenHash.Length == 0)
        {
            throw new ArgumentException(
                "Password reset token hash cannot be empty.",
                nameof(tokenHash));
        }

        return _dbContext.Users.SingleOrDefaultAsync(
            user => EF.Property<byte[]?>(user, "_passwordResetTokenHash") == tokenHash,
            cancellationToken);
    }

    /// <inheritdoc />
    public void Add(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _dbContext.Users.Add(user);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUnverifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        int deletedRows = await _dbContext.Users
            .Where(user =>
                user.Id == userId &&
                user.VerifiedAt == null)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows == 1;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateEmailViolation(exception))
        {
            // SQLSTATE 23505 is PostgreSQL's unique_violation code. This handles
            // concurrent registrations that both pass the preliminary email check
            // before one request violates the normalized-email unique index.
            throw new DuplicateUserEmailException(exception);
        }
    }

    private static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return email.Trim().ToUpperInvariant();
    }

    private static bool IsDuplicateEmailViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: NormalizedEmailUniqueIndexName
        };
    }
}
