using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using NojectServer.Tests.MockHelpers.AsyncQuerySupport;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.MockHelpers;

public static class DbSetMockHelper
{
    /// <summary>
    /// Creates a mock DbSet<T> for unit testing with support for querying, async operations, and basic CRUD.
    /// </summary>
    /// <typeparam name="T">The entity type (must be a class).</typeparam>
    /// <param name="data">The in-memory list of entities to use as the data source.</param>
    /// <returns>A configured Mock<DbSet<T>>.</returns>
    public static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Data list cannot be null.");

        var queryableData = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        // Setup IQueryable for LINQ queries
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryableData.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());

        // Setup IAsyncEnumerable for async operations
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryableData.GetEnumerator()));

        // Setup basic CRUD operations
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Returns<T, CancellationToken>(async (entity, token) =>
            {
                data.Add(entity);
                var mockEntityEntry = new Mock<EntityEntry<T>>();
                mockEntityEntry.Setup(e => e.Entity).Returns(entity);
                return await Task.FromResult(mockEntityEntry.Object);
            });
        mockSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(data.AddRange);
        mockSet.Setup(x => x.Remove(It.IsAny<T>())).Callback<T>(t => data.Remove(t));
        mockSet.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(ts =>
        {
            foreach (var t in ts) { data.Remove(t); }
        });

        // Setup async operations
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] keys) => data.FirstOrDefault());
        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken token) => data.FirstOrDefault());

        return mockSet;
    }
}
