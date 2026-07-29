using Microsoft.EntityFrameworkCore.Storage;
using NojectServer.Data;
using NojectServer.Modules.Identity.Application.Persistence;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Executes Identity operations within an EF Core transaction, committing
/// successful results and rolling back failures or exceptions.
/// </summary>
internal sealed class IdentityTransaction(
    DataContext dbContext) : IIdentityTransaction
{
    private readonly DataContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            Result result = await operation(cancellationToken);

            if (result.IsFailure)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                return result;
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }
}
