using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class OrderItemRepository : Repository<OrderItem>, IOrderItemRepository
    {
        private readonly MainDbContext _context;
        
        public OrderItemRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        //  Get order items by store ID with proper query building
        public async Task<IEnumerable<OrderItem>> GetOrderItemsByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            // Build query step by step to avoid conversion issues
            var baseQuery = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            // Apply date filters first
            if (fromDate.HasValue)
                baseQuery = baseQuery.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                baseQuery = baseQuery.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            // Apply includes after all filters
            var queryWithIncludes = baseQuery
                .Include(oi => oi.Order)
                    .ThenInclude(o => o!.User)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p!.Images.Where(img => !img.IsDeleted))
                .Include(oi => oi.Product)
                    .ThenInclude(p => p!.ProductStores.Where(ps => ps.StoreId == storeId && !ps.IsDeleted));

            return await queryWithIncludes
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync(cancellationToken);
        }

        //  Get order items by product ID with comprehensive data
        public async Task<IEnumerable<OrderItem>> GetOrderItemsByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var baseQuery = _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.ProductId == productId && !oi.IsDeleted);

            var queryWithIncludes = baseQuery
                .Include(oi => oi.Order)
                    .ThenInclude(o => o!.User)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p!.Images.Where(img => !img.IsDeleted));

            return await queryWithIncludes
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync(cancellationToken);
        }

        //  Get total quantity sold by store with date filtering
        public async Task<int> GetTotalQuantitySoldByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            //  Apply date filters
            if (fromDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            return await query.SumAsync(oi => oi.Quantity, cancellationToken);
        }

        //  Queryable access for complex queries
        public IQueryable<OrderItem> GetQueryable()
        {
            return _context.OrderItems.AsNoTracking();
        }

        //  Get total revenue by store ID
        public async Task<decimal> GetTotalRevenueByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            if (fromDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            return await query.SumAsync(oi => oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0)), cancellationToken);
        }

        //  Get top selling products by store
        public async Task<IEnumerable<OrderItem>> GetTopSellingProductsByStoreIdAsync(int storeId, int take = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var baseQuery = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            if (fromDate.HasValue)
                baseQuery = baseQuery.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                baseQuery = baseQuery.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            //  Get product quantity aggregation first, then get representative items
            var topProductIds = await baseQuery
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Take(take)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            // Get representative order items for top products
            var topProducts = new List<OrderItem>();
            foreach (var productId in topProductIds)
            {
                var representativeItem = await _context.OrderItems
                    .AsNoTracking()
                    .Where(oi => oi.ProductId == productId && !oi.IsDeleted)
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.Images.Where(img => !img.IsDeleted))
                    .FirstOrDefaultAsync(cancellationToken);

                if (representativeItem != null)
                {
                    topProducts.Add(representativeItem);
                }
            }

            return topProducts;
        }

        //  Get order status breakdown by store
        public async Task<Dictionary<string, int>> GetOrderStatusBreakdownByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var baseQuery = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            if (fromDate.HasValue)
                baseQuery = baseQuery.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                baseQuery = baseQuery.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            var queryWithOrder = baseQuery.Include(oi => oi.Order);

            return await queryWithOrder
                .GroupBy(oi => oi.Order!.OrderStatus)
                .ToDictionaryAsync(g => g.Key ?? "Unknown", g => g.Count(), cancellationToken);
        }

        //  Additional helper methods for better analytics
        public async Task<IEnumerable<OrderItem>> GetRecentOrderItemsByStoreIdAsync(int storeId, int take = 20, CancellationToken cancellationToken = default)
        {
            var baseQuery = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            var queryWithIncludes = baseQuery
                .Include(oi => oi.Order)
                    .ThenInclude(o => o!.User)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p!.Images.Where(img => !img.IsDeleted));

            return await queryWithIncludes
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetAverageOrderValueByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            if (fromDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            var orderTotals = await query
                .GroupBy(oi => oi.OrderId)
                .Select(g => g.Sum(oi => oi.TotalPrice - ((oi.EventDiscountAmount ?? 0) + (oi.RegularDiscountAmount ?? 0))))
                .ToListAsync(cancellationToken);

            return orderTotals.Any() ? orderTotals.Average() : 0;
        }

        public async Task<int> GetUniqueCustomerCountByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(oi => !oi.IsDeleted)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted));

            if (fromDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(oi => oi.Order!.OrderDate <= toDate.Value);

            return await query
                .Select(oi => oi.Order!.UserId)
                .Distinct()
                .CountAsync(cancellationToken);
        }
    }
}