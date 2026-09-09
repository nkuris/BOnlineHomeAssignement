using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;

namespace BOnlineHomeAssignement.Server.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository implementation providing standard CRUD operations.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        /// <summary>Get entity by primary key.</summary>
        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>Get all entities (untracked for read-only queries).</summary>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        /// <summary>Find entities matching predicate.</summary>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        /// <summary>Get single entity matching predicate or null.</summary>
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        /// <summary>Check if any entity matches predicate.</summary>
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>Count entities matching predicate.</summary>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return await (predicate == null 
                ? _dbSet.CountAsync() 
                : _dbSet.CountAsync(predicate));
        }

        /// <summary>Add entity to repository.</summary>
        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await SaveChangesAsync();
            return entity;
        }

        /// <summary>Add multiple entities.</summary>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities);
            await SaveChangesAsync();
        }

        /// <summary>Update entity.</summary>
        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await SaveChangesAsync();
        }

        /// <summary>Update multiple entities.</summary>
        public virtual async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentNullException(nameof(entities));

            _dbSet.UpdateRange(entities);
            await SaveChangesAsync();
        }

        /// <summary>Delete entity by id.</summary>
        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"Entity with id {id} not found");

            _dbSet.Remove(entity);
            await SaveChangesAsync();
        }

        /// <summary>Delete entity.</summary>
        public virtual async Task DeleteAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            await SaveChangesAsync();
        }

        /// <summary>Delete multiple entities.</summary>
        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentNullException(nameof(entities));

            _dbSet.RemoveRange(entities);
            await SaveChangesAsync();
        }

        /// <summary>Get paginated results.</summary>
        public virtual async Task<(IEnumerable<T> Items, int Total)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
        {
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 10;

            var query = _dbSet.AsNoTracking();

            // Apply filter
            if (predicate != null)
                query = query.Where(predicate);

            // Get total count before ordering and pagination
            var total = await query.CountAsync();

            // Apply ordering
            if (orderBy != null)
                query = orderBy(query);
            else
                query = query.OrderBy(x => x); // Default ordering if none specified

            // Apply pagination
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <summary>Save changes to database.</summary>
        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
