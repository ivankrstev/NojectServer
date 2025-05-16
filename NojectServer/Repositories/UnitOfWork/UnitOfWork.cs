using Microsoft.EntityFrameworkCore.Storage;
using NojectServer.Data;
using NojectServer.Repositories.Implementations;
using NojectServer.Repositories.Interfaces;

namespace NojectServer.Repositories.UnitOfWork;

/// <summary>
/// Implements the Unit of Work pattern to manage database transactions and repository access.
/// This class coordinates operations across multiple repositories and provides transaction management
/// to ensure data consistency.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _dataContext;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;
    public IUserRepository Users { get; private set; }
    public IProjectRepository Projects { get; private set; }
    public ICollaboratorRepository Collaborators { get; private set; }
    public ITaskRepository Tasks { get; private set; }
    public IRefreshTokenRepository RefreshTokens { get; private set; }

    public UnitOfWork(DataContext context)
    {
        _dataContext = context;
        Users = new UserRepository(_dataContext);
        Projects = new ProjectRepository(_dataContext);
        Collaborators = new CollaboratorRepository(_dataContext);
        Tasks = new TaskRepository(_dataContext);
        RefreshTokens = new RefreshTokenRepository(_dataContext);
    }

    /// <summary>
    /// Asynchronously begins a new database transaction if one doesn't already exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeginTransactionAsync()
    {
        _transaction ??= await _dataContext.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Asynchronously commits the current transaction and saves all changes to the database.
    /// After committing, the transaction is disposed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CommitTransactionAsync()
    {
        await _dataContext.SaveChangesAsync();
        await (_transaction?.CommitAsync() ?? Task.CompletedTask);
        await DisposeTransactionAsync();
    }

    /// <summary>
    /// Asynchronously disposes the current transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the UnitOfWork and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dataContext.Dispose();
                _transaction?.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _dataContext.Dispose();
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously releases the resources used by the UnitOfWork.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await DisposeAsyncCore();

            // Dispose managed resources that don't implement IAsyncDisposable
            Dispose(false);

            _disposed = true;

            // Suppress finalization
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Asynchronously releases the core resources used by the UnitOfWork.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_dataContext != null)
        {
            await _dataContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Asynchronously rolls back the current transaction, discarding all uncommitted changes.
    /// After rolling back, the transaction is disposed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollbackTransactionAsync()
    {
        await (_transaction?.RollbackAsync() ?? Task.CompletedTask);
        await DisposeTransactionAsync();
    }

    /// <summary>
    /// Asynchronously saves all changes made in this unit of work to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveChangesAsync() => await _dataContext.SaveChangesAsync();
}
