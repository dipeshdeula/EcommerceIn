using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MainDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
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
        private ISubCategoryRepository? _subCategories;
        private ISubSubCategoryRepository? _subSubCategories;
        private IUserRepository? _users;
        private IOrderRepository? _orders;
        private IOrderItemRepository? _orderItems;
        private ICartItemRepository? _cartItems;
        private IAddressRepository? _addresses;
        private IStoreRepository? _stores;
        private IStoreAddressRepository? _storeAddresses;
        private IPaymentMethodRepository? _paymentMethods;
        private IPaymentRequestRepository _paymentRequests;
        private ICompanyInfoRepository _companyInfo;
        private IBillingRepository _billing;
        private IBillingItemRepository? _billingItems;

        public UnitOfWork(MainDbContext context, ILogger<UnitOfWork> logger) 
        {
            _context = context;
            _logger = logger;

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

        public ISubCategoryRepository SubCategories =>
            _subCategories ??= new SubCategoryRepository(_context);

        public ISubSubCategoryRepository SubSubCategories =>
            _subSubCategories ??= new SubSubCategoryRepository(_context);

        public IUserRepository Users =>
            _users ??= new UserRepository(_context, null!); 

        public IOrderRepository Orders =>
            _orders ??= new OrderRepository(_context);

        public IOrderItemRepository OrderItems =>
            _orderItems ??= new OrderItemRepository(_context);

        public ICartItemRepository CartItems =>
            _cartItems ??= new CartItemRepository(_context);

        public IAddressRepository Addresses =>
            _addresses ??= new AddressRepository(_context);

        public IStoreRepository Stores =>
            _stores ??= new StoreRepository(_context);

        public IStoreAddressRepository StoreAddresses =>
            _storeAddresses ??= new StoreAddressRepository(_context);

        public IPaymentMethodRepository PaymentMethods =>
            _paymentMethods ??= new PaymentMethodRepository(_context);

        public IPaymentRequestRepository PaymentRequests => _paymentRequests ??= new PaymentRequestRepository(_context);
        public ICompanyInfoRepository CompanyInfos => _companyInfo ??= new CompanyInfoRepository(_context);
        public IBillingItemRepository BillingItems => _billingItems ??= new BillingItemRepository(_context);
        public IBillingRepository Billings => _billing ??= new BillingRepository(_context);


        // Transaction management
        /*   public async Task<IDbContextTransaction> BeginTransactionAsync()
           {
               if (_transaction == null)
               {
                   _transaction = await _context.Database.BeginTransactionAsync();
                   _logger.LogInformation("Transaction started");

               }
               return _transaction;
           }*/

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Changes saved successfully. {EntitiesChanged} entities changed", result);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict occurred while saving changes");
                throw new InvalidOperationException("Concurrency conflict occurred", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed while saving changes");
                throw new InvalidOperationException("Database update failed", ex);
            }
        }
        public async Task<int> SaveChangesWithRetryAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
        {
            var retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    return await SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    _logger.LogWarning("Concurrency conflict, retrying... Attempt {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                    await Task.Delay(100 * retryCount, cancellationToken); // Exponential backoff
                }
            }
            throw new InvalidOperationException($"Failed to save changes after {maxRetries} retries");
        }

       /* public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully");
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
*/
       /* public async Task RollbackTransactionAsync()
        {
           if (_transaction != null)
            {
                try
                {
                    await _transaction.RollbackAsync();
                    _logger.LogWarning("Transaction rolled back");
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
*/
         // ✅ Advanced transaction helpers
         public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Transaction completed successfully");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed and was rolled back");
                    throw;
                }
            });
        }
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Transaction completed successfully");
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed and was rolled back");
                    throw;
                }
            });
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
