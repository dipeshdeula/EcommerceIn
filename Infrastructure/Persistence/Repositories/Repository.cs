using Application.Interfaces.Repositories;
using System.Linq.Expressions;
using Application.Extension;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext _dbContext;

        public Repository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region Query Methods

        public virtual IQueryable<TEntity> Queryable
        {
            get
            {
                return GetQueryable();
            }
        }

        public virtual IQueryable<TEntity> GetQueryable()
        {
            return _dbContext.Set<TEntity>();
        }

        public virtual IQueryable<TEntity> GetQueryable(bool includeDeleted)
        {
            var query = _dbContext.Set<TEntity>().AsQueryable();
            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }
            return query;
        }

        #endregion

        #region Find Methods

        public TEntity FindById(object id)
        {
            return _dbContext.Set<TEntity>().Find(id);
        }

        public virtual async Task<TEntity> FindByIdAsync(object id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

        public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbContext.Set<TEntity>().Where(predicate).FirstOrDefault();
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbContext.Set<TEntity>().Where(predicate).FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, string>> orderBy, string sortDirection = "")
        {
            switch (sortDirection.ToLower())
            {
                case "desc":
                    return await _dbContext.Set<TEntity>().Where(predicate).OrderByDescending(orderBy).FirstOrDefaultAsync();
                default:
                    return await _dbContext.Set<TEntity>().Where(predicate).OrderBy(orderBy).FirstOrDefaultAsync();
            }
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> orderBy, string sortDirection = "")
        {
            switch (sortDirection.ToLower())
            {
                case "desc":
                    return await _dbContext.Set<TEntity>().Where(predicate).OrderByDescending(orderBy).FirstOrDefaultAsync();
                default:
                    return await _dbContext.Set<TEntity>().Where(predicate).OrderBy(orderBy).FirstOrDefaultAsync();
            }
        }

        public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, DateTime>> orderBy, string sortDirection = "")
        {
            switch (sortDirection.ToLower())
            {
                case "desc":
                    return await _dbContext.Set<TEntity>().Where(predicate).OrderByDescending(orderBy).FirstOrDefaultAsync();
                default:
                    return await _dbContext.Set<TEntity>().Where(predicate).OrderBy(orderBy).FirstOrDefaultAsync();
            }
        }

        #endregion

        #region Get Methods

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null, bool includeDeleted = false)
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
            bool includeDeleted = false)
        {
            var query = GetQueryable(includeDeleted);

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (take.HasValue)
                query = query.Take(take.Value);

            return await query.ToListAsync();
        }

        #endregion

        #region Add Methods

        public TEntity Add(TEntity entity)
        {
            _dbContext.Set<TEntity>().Add(entity);
            return entity;
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken); // Ensure this line is present
            return entity;
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            _dbContext.Set<TEntity>().AddRange(entities);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        }

        #endregion

        #region Update Methods

        public TEntity Update(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
            return entity;
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                _dbContext.Entry(entity).State = EntityState.Modified;
            }
        }

        public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                _dbContext.Entry(entity).State = EntityState.Modified;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        #endregion

        #region Delete Methods

        public void Remove(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
        }

        public async Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            _dbContext.Set<TEntity>().RemoveRange(entities);
        }

        public async Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            _dbContext.Set<TEntity>().RemoveRange(entities);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        #endregion

        #region Soft Delete Methods

        public async Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.SoftDeleteAsync(entity, cancellationToken);
        }

        public async Task SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await _dbContext.SoftDeleteAsync(entity, cancellationToken);
            }
        }

        public async Task<bool> UndeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return await _dbContext.UndeleteAsync(entity, cancellationToken);
        }

        #endregion

        #region Check Methods

        public bool Any()
        {
            return _dbContext.Set<TEntity>().Any();
        }

        public bool Any(Expression<Func<TEntity, bool>> where)
        {
            return _dbContext.Set<TEntity>().Any(where);
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.Set<TEntity>().AnyAsync(cancellationToken);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default)
        {
            return _dbContext.Set<TEntity>().AnyAsync(where, cancellationToken);
        }

        #endregion

        #region Count Methods

        public int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
                return _dbContext.Set<TEntity>().Count();
            else
                return _dbContext.Set<TEntity>().Count(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                return await _dbContext.Set<TEntity>().CountAsync(cancellationToken);
            else
                return await _dbContext.Set<TEntity>().CountAsync(predicate, cancellationToken);
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

       

        #endregion
    }
}