using System.Linq.Expressions;

namespace NojectServer.Repositories.Base;

/// <summary>
/// Defines a generic repository interface for performing common data access operations.
/// This interface provides a standard set of methods for working with any entity type,
/// abstracting the underlying data access technology.
/// </summary>
/// <typeparam name="T">The entity type the repository works with.</typeparam>
public interface IGenericRepository<T> where T : class
{
    /// <summary>
    /// Asynchronously retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Asynchronously finds all entities that satisfy the specified expression.
    /// </summary>
    /// <param name="expression">The predicate to filter entities.</param>
    /// <returns>A collection of entities that match the criteria.</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);

    /// <summary>
    /// Asynchronously determines whether any entity satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test entities against.</param>
    /// <returns>True if any entity satisfies the condition; otherwise, false.</returns>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(T entity);

    /// <summary>
    /// Asynchronously adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Marks an entity for deletion.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(T entity);

    /// <summary>
    /// Marks multiple entities for deletion.
    /// </summary>
    /// <param name="entities">The collection of entities to remove.</param>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>
    /// Asynchronously removes an entity by its ID after first retrieving it from the database.
    /// This is a safer method than direct removal as it ensures the entity exists.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to remove.</param>
    /// <returns>True if the entity was found and removed; otherwise, false.</returns>
    Task<bool> SafeRemoveAsync(string id);

    /// <summary>
    /// Marks an entity as modified for an update operation.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>
    /// Provides direct queryable access to the entities for more complex operations.
    /// </summary>
    /// <returns>An IQueryable interface for the entity type.</returns>
    IQueryable<T> Query();
}
