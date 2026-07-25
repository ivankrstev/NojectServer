using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserRepository(DataContext dbContext) : IUserRepository
{
    private readonly DataContext _dbContext = dbContext;

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
    public void Add(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _dbContext.Users.Add(user);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return email.Trim().ToUpperInvariant();
    }
}
