using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities.Common;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;

namespace Infrastructure.Persistence.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly MainDbContext _dbContext;
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(MainDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<TEntity>();
        }

        #region Query Methods

        public virtual IQueryable<TEntity> Queryable => _dbSet;


        public virtual IQueryable<TEntity> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public virtual IQueryable<TEntity> GetQueryable(bool includeDeleted)
        {
            var query = _dbSet.AsQueryable();
            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }
            return query;
        }
        public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public virtual async Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            string? includeProperties = null,
            bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = GetQueryable(includeDeleted);

            // Apply predicate
            query = query.Where(predicate);

            // Include related properties
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, int? skip = null, int? take = null, bool includeDeleted = false, string includeProperties = null, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = GetQueryable(includeDeleted);

            // Include navigation properties
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            // Apply additional filtering
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Apply pagination
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }


        public async Task<IEnumerable<TEntity>> GetWithIncludesAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            // Apply includes first for better query optimization
            if (includes != null && includes.Length > 0)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            // Apply filter after includes
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering last
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync();
        }

        //  Overload with string-based includes for flexibility
        public async Task<IEnumerable<TEntity>> GetWithIncludesAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = null)
        {
            IQueryable<TEntity> query = _dbSet;

            // Apply string-based includes
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            // Apply filter
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync();
        }
        public async Task<IEnumerable<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            int pageNumber = 1,
            int pageSize = 10,
            bool includeDeleted = false,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = GetQueryable(includeDeleted);

            // Apply includes
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            // Apply filter
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Apply pagination
            var skip = (pageNumber - 1) * pageSize;
            return await query.Skip(skip).Take(pageSize).ToListAsync();
        }

        #endregion

        #region Find Methods

        public TEntity FindById(object id)
        {
            return _dbSet.Find(id);
        }

        public virtual async Task<TEntity> FindByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.Where(predicate).FirstOrDefault();
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.Where(predicate).FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, string>> orderBy, string sortDirection = "")
        {
            switch (sortDirection.ToLower())
            {
                case "desc":
                    return await _dbSet.Where(predicate).OrderByDescending(orderBy).FirstOrDefaultAsync();
                default:
                    return await _dbSet.Where(predicate).OrderBy(orderBy).FirstOrDefaultAsync();
            }
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> orderBy, string sortDirection = "")
        {
            switch (sortDirection.ToLower())
            {
                case "desc":
                    return await _dbSet.Where(predicate).OrderByDescending(orderBy).FirstOrDefaultAsync();
                default:
                    return await _dbSet.Where(predicate).OrderBy(orderBy).FirstOrDefaultAsync();
            }
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, DateTime>> orderBy, string sortDirection = "")
        {
            switch (sortDirection.ToLower())
            {
                case "desc":
                    return await _dbSet.Where(predicate).OrderByDescending(orderBy).FirstOrDefaultAsync();
                default:
                    return await _dbSet.Where(predicate).OrderBy(orderBy).FirstOrDefaultAsync();
            }
        }

        #endregion

        #region Get Methods

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, 
            bool>> predicate = null, 
            bool includeDeleted = false
            )
        {
            var query = GetQueryable(includeDeleted);

            if (predicate != null)
                query = query.Where(predicate);

            return await query.ToListAsync();
        }

        public async virtual Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            int? skip = null,
            int? take = null,
            bool includeDeleted = false,
            string includeProperties = null)

        {
          

            IQueryable<TEntity> query = GetQueryable(includeDeleted);

            // Include navigation properties
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }          

            // Apply additional filtering
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Apply pagination
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        #endregion

        #region Add Methods

        public TEntity Add(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Add(entity);
            return entity;
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            await _dbSet.AddAsync(entity, cancellationToken);
            
            //await _dbContext.SaveChangesAsync(cancellationToken); // Ensure this line is present
            return entity;
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            _dbSet.AddRange(entities);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            await _dbSet.AddRangeAsync(entities, cancellationToken);
        }

        #endregion

        #region Update Methods

        public TEntity Update(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            _dbSet.Update(entity);
            return entity;
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            // foreach (var entity in entities)
            // {
            //     _dbContext.Entry(entity).State = EntityState.Modified;
            // }
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            _dbSet.UpdateRange(entities);
        }

        public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            //_dbContext.Entry(entity).State = EntityState.Modified;
            //await _dbContext.SaveChangesAsync(cancellationToken);
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            _dbSet.Update(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            /*foreach (var entity in entities)
            {
                _dbContext.Entry(entity).State = EntityState.Modified;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);*/
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }        

        #endregion

        #region Delete Methods

        public void Remove(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            _dbSet.Remove(entity);
        }

        public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            return Task.CompletedTask;
            // await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            _dbSet.RemoveRange(entities);
        }

        public Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }
            _dbSet.RemoveRange(entities);
            // await _dbContext.SaveChangesAsync(cancellationToken);
            return Task.CompletedTask;
        }

        #endregion

        #region Soft Delete Methods

        public async Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            // await _dbContext.SoftDeleteAsync(entity, cancellationToken);
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            await _dbContext.SoftDeleteAsync(entity, cancellationToken);



        }

        public async Task SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {

            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                await _dbContext.SoftDeleteAsync(entity, cancellationToken);
            }
        }

        public async Task<bool> UndeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                return false;

            return await _dbContext.UndeleteAsync(entity, cancellationToken);



        }
        public async Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbContext.HardDeleteAsync(entity, cancellationToken);
        }

        public async Task HardDeleteByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await _dbContext.HardDeleteByIdAsync<TEntity>(id, cancellationToken);
        }

        #endregion

        #region Check Methods

        public bool Any()
        {
            return _dbSet.Any();
        }

        public bool Any(Expression<Func<TEntity, bool>> where)
        {
            return _dbSet.Any(where);
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return _dbSet.AnyAsync(cancellationToken);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default)
        {
            return _dbSet.AnyAsync(where, cancellationToken);
        }

        #endregion

        #region Count Methods

        public int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
                return _dbSet.Count();
            else
                return _dbSet.Count(predicate);
        }

      public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
{
    try
    {
        IQueryable<TEntity> query = _dbContext.Set<TEntity>();

        // ✅ Apply predicate only if not null
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        // Log error if logger is available
        throw new InvalidOperationException($"Error counting entities: {ex.Message}", ex);
    }
}

        #endregion

        #region SaveChanges Methods

        public int SaveChanges()
        {
            return _dbContext.SaveChanges();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }


        protected IQueryable<T> GetDbSet<T>() where T : class
        {
            return _dbContext.Set<T>();
        }
        // Access to specific DbSets
        protected DbSet<BannerEventSpecial> BannerEventSpecials => _dbContext.BannerEventSpecials;
        protected DbSet<EventProduct> EventProducts => _dbContext.EventProducts;
        protected DbSet<EventUsage> EventUsages => _dbContext.EventUsages;
        protected DbSet<Product> Products => _dbContext.Products;


        #endregion
    }
}