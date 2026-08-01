namespace NojectServer.Shared.Application.Persistence;

/// <summary>
/// Commits changes staged across repositories that share the same DbContext.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes tracked by the shared DbContext as a
    /// single atomic transaction.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
