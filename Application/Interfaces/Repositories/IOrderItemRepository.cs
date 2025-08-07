using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IOrderItemRepository : IRepository<OrderItem>
    {
         Task<IEnumerable<OrderItem>> GetOrderItemsByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderItem>> GetOrderItemsByProductIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<int> GetTotalQuantitySoldByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        
        // Advanced analytics methods
        Task<decimal> GetTotalRevenueByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderItem>> GetTopSellingProductsByStoreIdAsync(int storeId, int take = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        Task<Dictionary<string, int>> GetOrderStatusBreakdownByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        
        // Additional helper methods
        Task<IEnumerable<OrderItem>> GetRecentOrderItemsByStoreIdAsync(int storeId, int take = 20, CancellationToken cancellationToken = default);
        Task<decimal> GetAverageOrderValueByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        Task<int> GetUniqueCustomerCountByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        
        // Queryable access for complex queries
        IQueryable<OrderItem> GetQueryable();


    
    }
}
