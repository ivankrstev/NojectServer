using NojectServer.Data;
using NojectServer.Shared.Application.Persistence;

namespace NojectServer.Shared.Infrastructure.Persistence;

/// <summary>
/// Commits changes across repositories by delegating to the shared DbContext.
/// Intended for services that coordinate more than one repository in a
/// single atomic operation; single-repository services should keep using
/// that repository's own SaveChangesAsync instead.
/// </summary>
internal sealed class UnitOfWork(DataContext dbContext) : IUnitOfWork
{
    private readonly DataContext _dbContext = dbContext;

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
