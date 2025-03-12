using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace NojectServer.Tests.MockHelpers.AsyncQuerySupport;

/// <summary>
/// A test implementation of <see cref="IAsyncQueryProvider"/> for use in unit tests that enables mocking
/// of Entity Framework Core's asynchronous query capabilities. This provider translates between synchronous
/// LINQ operations on in-memory collections and the asynchronous query interfaces expected by EF Core.
/// </summary>
/// <typeparam name="TEntity">The type of entities being queried.</typeparam>
/// <param name="inner">The underlying <see cref="IQueryProvider"/> that will execute the synchronous portions of the queries.
/// This is typically the query provider from an in-memory collection that the async operations will delegate to.</param>
internal class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>
    /// Constructs an <see cref="IQueryable"/> for the specified expression.
    /// </summary>
    /// <param name="expression">An expression representing a query.</param>
    /// <returns>An <see cref="IQueryable"/> that can evaluate the query represented by the specified expression.</returns>
    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    /// <summary>
    /// Constructs an <see cref="IQueryable{TElement}"/> for the specified expression.
    /// </summary>
    /// <typeparam name="TElement">The element type of the query.</typeparam>
    /// <param name="expression">An expression representing a query.</param>
    /// <returns>An <see cref="IQueryable{TElement}"/> that can evaluate the query represented by the specified expression.</returns>
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    /// <summary>
    /// Executes the query represented by a specified expression.
    /// </summary>
    /// <param name="expression">An expression representing a query.</param>
    /// <returns>The value resulting from executing the specified query. Throws if the result is null.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query execution returns null.</exception>
    public object Execute(Expression expression)
    {
        return _inner.Execute(expression) ?? throw new InvalidOperationException("Execution returned null.");
    }

    /// <summary>
    /// Executes the strongly-typed query represented by a specified expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
    /// <param name="expression">An expression representing a query.</param>
    /// <returns>The value resulting from executing the specified query.</returns>
    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    /// <summary>
    /// Executes the query asynchronously and returns the results as an <see cref="IAsyncEnumerable{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The element type of the result sequence.</typeparam>
    /// <param name="expression">An expression representing a query.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> containing the results of the query.</returns>
    public static IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        return new TestAsyncEnumerable<TResult>(expression);
    }

    /// <summary>
    /// Asynchronously executes the query represented by a specified expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
    /// <param name="expression">An expression representing a query.</param>
    /// <param name="_">A cancellation token that is ignored in this implementation.</param>
    /// <returns>A task representing the asynchronous operation and containing the value resulting from executing the specified query.</returns>
    public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken _)
    {
        return Task.FromResult(Execute<TResult>(expression));
    }

    /// <summary>
    /// Implements the <see cref="IAsyncQueryProvider.ExecuteAsync{TResult}"/> method to handle special cases like
    /// <c>ExecuteDelete</c> operations and other asynchronous queries.
    /// </summary>
    /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
    /// <param name="expression">An expression representing a query.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// An object representing the results of the query. For delete operations, returns the count of affected records.
    /// For other operations, returns the query result wrapped in a task.
    /// </returns>
    TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        // Check if it's a delete operation
        if (typeof(TResult) == typeof(Task<int>) && expression is MethodCallExpression methodCall
                    && methodCall.Method.Name == "ExecuteDelete")
        {
            var source = _inner.CreateQuery<TEntity>(methodCall.Arguments[0]);
            var itemsToDelete = source.ToList();
            return (TResult)(object)Task.FromResult(itemsToDelete.Count); // Simulate deletion count
        }
        var resultType = typeof(TResult).GetGenericArguments()[0]; // Get the inner type of Task<T>
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(_inner, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}