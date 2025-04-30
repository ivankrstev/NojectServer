using NojectServer.Repositories.Interfaces;

namespace NojectServer.Repositories.UnitOfWork;

/// <summary>
/// Defines the contract for a Unit of Work implementation that manages database transactions and repository access.
/// The Unit of Work pattern coordinates operations across multiple repositories and ensures
/// transactional consistency when handling multiple database operations.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IProjectRepository Projects { get; }
    ICollaboratorRepository Collaborators { get; }
    ITaskRepository Tasks { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>
    /// Asynchronously saves all changes made in this unit of work to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveChangesAsync();

    /// <summary>
    /// Asynchronously begins a new database transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeginTransactionAsync();

    /// <summary>
    /// Asynchronously commits the current transaction and saves all changes to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitTransactionAsync();

    /// <summary>
    /// Asynchronously rolls back the current transaction, discarding all uncommitted changes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackTransactionAsync();
}
