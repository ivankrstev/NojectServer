namespace NojectServer.Tests.MockHelpers.AsyncQuerySupport;

/// <summary>
/// A test implementation of <see cref="IAsyncEnumerator{T}"/> that wraps a synchronous enumerator
/// to provide asynchronous enumeration capabilities for unit testing.
/// </summary>
/// <typeparam name="T">The type of elements to enumerate.</typeparam>
/// <param name="inner">The underlying synchronous <see cref="IEnumerator{T}"/> that this async enumerator will delegate to.
/// All operations performed by this asynchronous enumerator will be executed on this inner enumerator.</param>
internal class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>
    /// Gets the element in the collection at the current position of the enumerator.
    /// </summary>
    public T Current => _inner.Current;

    /// <summary>
    /// Advances the enumerator asynchronously to the next element of the collection.
    /// This implementation synchronously calls MoveNext on the inner enumerator and wraps the result
    /// in a ValueTask.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{Boolean}"/> that will complete with a result of <c>true</c> if the enumerator
    /// was successfully advanced to the next element, or <c>false</c> if the enumerator has passed the end
    /// of the collection.
    /// </returns>
    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        _inner.Dispose(); // Dispose the inner enumerator
        GC.SuppressFinalize(this); // Suppress finalization for performance
        return ValueTask.CompletedTask;
    }
}
