using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Persistence;

/// <summary>
/// Defines a transaction boundary for Identity use cases that coordinate
/// multiple persistence operations.
/// </summary>
public interface IIdentityTransaction
{
    /// <summary>
    /// Executes the provided operation within a database transaction.
    /// </summary>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <param name="cancellationToken">A token used to cancel the database operation.</param>
    /// <returns>
    /// A Result indicating whether the operation completed successfully.
    /// If the operation fails, the transaction is rolled back and the failure result is returned.
    /// </returns>
    Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default);
}
