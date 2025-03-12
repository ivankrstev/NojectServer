using System.Linq.Expressions;

namespace NojectServer.Tests.MockHelpers.AsyncQuerySupport;

/// <summary>
/// A test implementation of <see cref="IQueryable{T}"/> and <see cref="IAsyncEnumerable{T}"/> for use in unit tests.
/// This class enables mocking of Entity Framework Core's asynchronous querying capabilities by providing a bridge
/// between synchronous LINQ operations on in-memory collections and the asynchronous query interfaces expected by
/// EF Core. It supports both query expression building and asynchronous enumeration over test data collections
/// without requiring a database connection.
/// </summary>
/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAsyncEnumerable{T}"/> class
    /// with the specified expression.
    /// </summary>
    /// <param name="expression">The expression that describes the query.</param>
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAsyncEnumerable{T}"/> class
    /// with the specified enumerable source.
    /// </summary>
    /// <param name="enumerable">The enumerable data source.</param>
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

    /// <summary>
    /// Gets an asynchronous enumerator over the elements in the collection.
    /// </summary>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous enumeration.</param>
    /// <returns>An <see cref="IAsyncEnumerator{T}"/> that can be used to iterate asynchronously over the collection.</returns>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    /// <summary>
    /// Gets the query provider that is associated with this data source.
    /// Returns a test implementation that supports asynchronous operations.
    /// </summary>
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}
