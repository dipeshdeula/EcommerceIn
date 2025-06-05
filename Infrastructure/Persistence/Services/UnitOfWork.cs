using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MainDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repository instances
        private IBannerEventSpecialRepository? _bannerEventSpecials;
        private IEventRuleRepository? _eventRules;
        private IEventProductRepository? _eventProducts;
        private IBannerImageRepository? _bannerImages;
        private IEventUsageRepository? _eventUsages;
        private IProductRepository? _products;
        private ICategoryRepository? _categories;

        public UnitOfWork(MainDbContext context)
        {
            _context = context;
        }

        // Lazy-loaded repositories
        public IBannerEventSpecialRepository BannerEventSpecials =>
            _bannerEventSpecials ??= new BannerEventSpecialRepository(_context);

        public IEventRuleRepository EventRules =>
            _eventRules ??= new EventRuleRepository(_context);

        public IEventProductRepository EventProducts =>
            _eventProducts ??= new EventProductRepository(_context);

        public IBannerImageRepository BannerImages =>
            _bannerImages ??= new BannerImageRepository(_context);

        public IEventUsageRepository EventUsages =>
            _eventUsages ??= new EventUsageRepository(_context);

        public IProductRepository Products =>
            _products ??= new ProductRepository(_context,null!);

        public ICategoryRepository Categories =>
            _categories ??= new CategoryRepository(_context);

        // Transaction management
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
            return _transaction;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new InvalidOperationException("Concurrency conflict occurred", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database update errors
                throw new InvalidOperationException("Database update failed", ex);
            }
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Bulk operations for performance
        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _context.Set<T>().AddRangeAsync(entities);
            await SaveChangesAsync();
        }

        public async Task BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class
        {
            _context.Set<T>().UpdateRange(entities);
            await SaveChangesAsync();
        }

        // Dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
                _disposed = true;
            }
        }
    }
}
