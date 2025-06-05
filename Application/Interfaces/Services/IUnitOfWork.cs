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

        // Transaction management
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Bulk operations
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class;
        Task BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class;
    }
}
