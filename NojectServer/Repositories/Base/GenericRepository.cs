using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using System.Linq.Expressions;

namespace NojectServer.Repositories.Base;

/// <summary>
/// Implements the generic repository pattern for data access operations using Entity Framework Core.
/// This class provides standard CRUD operations and query capabilities for any entity type,
/// abstracting the underlying data access details.
/// </summary>
/// <typeparam name="T">The entity type the repository works with.</typeparam>
/// <typeparam name="TId">The type of the entity's primary key.</typeparam>
public class GenericRepository<T, TId>(DataContext dataContext) : IGenericRepository<T, TId> where T : class
{
    protected readonly DbContext _dataContext = dataContext;
    protected readonly DbSet<T> _dbSet = dataContext.Set<T>();

    /// <summary>
    /// Asynchronously adds a new entity to the database context.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    /// <summary>
    /// Asynchronously adds multiple entities to the database context.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);

    /// <summary>
    /// Asynchronously determines whether any entity satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test entities against.</param>
    /// <returns>True if any entity satisfies the condition; otherwise, false.</returns>
    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

    /// <summary>
    /// Asynchronously finds all entities that satisfy the specified expression.
    /// </summary>
    /// <param name="expression">The predicate to filter entities.</param>
    /// <returns>A collection of entities that match the criteria.</returns>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
        => await _dbSet.Where(expression).ToListAsync();

    /// <summary>
    /// Asynchronously retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    public virtual async Task<T?> GetByIdAsync(TId id) => await _dbSet.FindAsync(id);

    /// <summary>
    /// Provides direct queryable access to the entities for more complex operations.
    /// </summary>
    /// <returns>An IQueryable interface for the entity type.</returns>
    public IQueryable<T> Query() => _dbSet.AsQueryable();

    /// <summary>
    /// Marks an entity for deletion in the database context.
    /// Handles detached entities by first attaching them to the context.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public virtual void Remove(T entity)
    {
        // Check if the entity is already attached to the context. If not, attach it.
        // to avoid the "The instance of entity type 'T' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked."
        if (_dataContext.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Marks multiple entities for deletion in the database context.
    /// Handles detached entities by first attaching them to the context.
    /// </summary>
    /// <param name="entities">The collection of entities to remove.</param>
    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            if (_dataContext.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
        }
        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Asynchronously removes an entity by its ID after first retrieving it from the database.
    /// This is a safer method than direct removal as it ensures the entity exists.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to remove.</param>
    /// <returns>True if the entity was found and removed; otherwise, false.</returns>
    public virtual async Task<bool> SafeRemoveAsync(TId id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
        {
            return false;
        }
        _dbSet.Remove(entity);
        return true;
    }

    /// <summary>
    /// Marks an entity as modified for an update operation.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public virtual void Update(T entity)
    {
        _dbSet.Attach(entity);
        _dataContext.Entry(entity).State = EntityState.Modified;
    }
}
