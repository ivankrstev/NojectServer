using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;

namespace NojectServer.Tests;

public static class DbSetMockHelper
{
    public static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        // Set up synchronous operations
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        // Set up CRUD operations
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>((entity) => data.Add(entity));
        mockSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(entities =>
        {
            foreach (var entity in entities)
            {
                data.Add(entity);
            }
        });
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>((entity) => data.Remove(entity));
        mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(entities =>
        {
            foreach (var entity in entities)
            {
                data.Remove(entity);
            }
        });

        // Set up async operations
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] keys) => data.FirstOrDefault());
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken token) => data.FirstOrDefault());

        // If async operations are used, set up async provider and enumerator:
        mockSet.As<IAsyncEnumerable<T>>()
               .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
               .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
               .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        return mockSet;
    }

    /// <summary>
    /// A test implementation of IAsyncEnumerator for use in unit tests
    /// </summary>
    public class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner = inner;

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// A test implementation of IAsyncQueryProvider for use in unit tests
    /// </summary>
    public class TestAsyncQueryProvider<T>(IQueryProvider inner) : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner = inner;

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<object>(expression, _inner);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression, _inner);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression) ?? throw new InvalidOperationException("Execution returned null.");
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            // For async operations, we need to first check what type of task we're dealing with
            Type resultType = typeof(TResult);

            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Extract the actual result type from Task<T>
                Type actualResultType = resultType.GetGenericArguments()[0];

                // Get the Execute<T> method on the inner provider
                var executeMethod = typeof(IQueryProvider)
                    .GetMethods()
                    .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                    .MakeGenericMethod(actualResultType);

                // Execute the query synchronously
                var result = executeMethod.Invoke(_inner, [expression]);

                // Create a completed task with the result
                var fromResultMethod = (typeof(Task)
                    .GetMethod(nameof(Task.FromResult))
                    ?.MakeGenericMethod(actualResultType)) ?? throw new InvalidOperationException("Unable to create a Task from the result.");
                return (TResult)fromResultMethod.Invoke(null, [result])!;
            }

            // If we're dealing with a non-generic Task, just return a completed task
            if (resultType == typeof(Task))
            {
                var result = _inner.Execute(expression);
                return (TResult)(object)Task.CompletedTask;
            }

            // For non-task results, just execute synchronously
            return _inner.Execute<TResult>(expression);
        }
    }

    /// <summary>
    /// A test implementation of IQueryable and IAsyncEnumerable for use in unit tests
    /// </summary>
    public class TestAsyncEnumerable<T> : IQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly IQueryable<T> _queryable;

        public TestAsyncEnumerable(Expression expression, IQueryProvider provider)
        {
            _queryable = provider.CreateQuery<T>(expression);
        }

        public TestAsyncEnumerable(IEnumerable<T> enumerable)
        {
            _queryable = enumerable.AsQueryable();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(_queryable.GetEnumerator());
        }

        public Type ElementType => _queryable.ElementType;

        public Expression Expression => _queryable.Expression;

        public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_queryable.Provider);

        public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
