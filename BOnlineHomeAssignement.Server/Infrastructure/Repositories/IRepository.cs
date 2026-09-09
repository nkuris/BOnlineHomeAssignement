using System.Linq.Expressions;

namespace BOnlineHomeAssignement.Server.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository interface for standard CRUD operations.
    /// All entity repositories should implement this interface.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>Get entity by primary key.</summary>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>Get all entities (untracked for read-only queries).</summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>Find entities matching predicate.</summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Get single entity matching predicate or null.</summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Check if any entity matches predicate.</summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Count entities matching predicate.</summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>Add entity to repository.</summary>
        Task<T> AddAsync(T entity);

        /// <summary>Add multiple entities.</summary>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>Update entity.</summary>
        Task UpdateAsync(T entity);

        /// <summary>Update multiple entities.</summary>
        Task UpdateRangeAsync(IEnumerable<T> entities);

        /// <summary>Delete entity by id.</summary>
        Task DeleteAsync(Guid id);

        /// <summary>Delete entity.</summary>
        Task DeleteAsync(T entity);

        /// <summary>Delete multiple entities.</summary>
        Task DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>Get paginated results.</summary>
        Task<(IEnumerable<T> Items, int Total)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

        /// <summary>Save changes to database.</summary>
        Task SaveChangesAsync();
    }
}
