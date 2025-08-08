using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository access
        IBannerEventSpecialRepository BannerEventSpecials { get; }
        IEventRuleRepository EventRules { get; }
        IEventProductRepository EventProducts { get; }
        IBannerImageRepository BannerImages { get; }
        IEventUsageRepository EventUsages { get; }
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        ISubCategoryRepository SubCategories { get; }
        ISubSubCategoryRepository SubSubCategories { get; }
        IUserRepository Users { get; }
        IOrderRepository Orders { get; }
        IOrderItemRepository OrderItems { get; }
        public ICartItemRepository CartItems { get; }
        IAddressRepository Addresses { get; }
        IStoreRepository Stores { get; }
        IStoreAddressRepository StoreAddresses { get; }
        IPaymentMethodRepository PaymentMethods { get; }
        IPaymentRequestRepository PaymentRequests { get; }
        ICompanyInfoRepository CompanyInfos { get; }
        IBillingRepository Billings { get; }
        IBillingItemRepository BillingItems { get; }
        IServiceAreaRepository ServiceAreas { get; }
        IPromoCodeRepository PromoCodes { get; }
   

        // Transaction management
        // Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        //Task CommitTransactionAsync();
        //Task RollbackTransactionAsync();

        // Bulk operations
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class;
        Task BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class;

        
        //  Advanced operations
        Task<int> SaveChangesWithRetryAsync(int maxRetries = 3, CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(Func<Task> operation);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}
