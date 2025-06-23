using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.Interfaces.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        // Query methods
        IQueryable<TEntity> Queryable { get; }
        IQueryable<TEntity> GetQueryable();
        IQueryable<TEntity> GetQueryable(bool includeDeleted);

        // ✅ Single entity operations
        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        // ✅ ADDED: Missing GetAsync method
        Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            string? includeProperties = null,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);


        // Find methods
        TEntity FindById(object id);
        Task<TEntity> FindByIdAsync(object id);
        TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, string>> orderBy, string sortDirection = "");
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> orderBy, string sortDirection = "");
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, DateTime>> orderBy, string sortDirection = "");

        // Get methods
        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null, bool includeDeleted = false);
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            int? skip = null,
            int? take = null,
            bool includeDeleted = false,
            string includeProperties = null
            );
        //Task<IEnumerable<TEntity>> GetAllAsync(
        //    Expression<Func<TEntity, bool>> predicate = null,
        //    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        //    int? skip = null,
        //    int? take = null,
        //    bool includeDeleted = false,
        //    string includeProperties = null,
        //    CancellationToken cancellationToken = default
        //    );
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            int? skip = null,
            int? take = null,
            bool includeDeleted = false,
            string includeProperties = null,
            CancellationToken cancellationToken = default
            );
        Task<IEnumerable<TEntity>> GetWithIncludesAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params Expression<Func<TEntity, object>>[] includes);

        Task<IEnumerable<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            int pageNumber = 1,
            int pageSize = 10,
            bool includeDeleted = false,
            params Expression<Func<TEntity, object>>[] includes);
        // Add methods
        TEntity Add(TEntity entity);
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        void AddRange(IEnumerable<TEntity> entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        // Update methods
        TEntity Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);       
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        // Delete methods
        void Remove(TEntity entity);
        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
        void RemoveRange(IEnumerable<TEntity> entities);
        Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        // Soft delete methods (for entities with IsDeleted property)
        Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<bool> UndeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        // Check methods
        bool Any();
        bool Any(Expression<Func<TEntity, bool>> where);
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);

        // Count methods
        int Count(Expression<Func<TEntity, bool>> predicate = null);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken cancellationToken = default);

        // Save changes
        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}