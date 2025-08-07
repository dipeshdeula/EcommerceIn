using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        private readonly MainDbContext _context;

        public OrderRepository(MainDbContext dbcontext) : base(dbcontext)
        {
            _context = dbcontext;

        }
        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate && !o.IsDeleted)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate) 
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Where(o => o.Items.Any(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted)));

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate <= toDate.Value);

            return await query
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate) 
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetTotalRevenueByStoreIdAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Where(o => o.Items.Any(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted)));

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate <= toDate.Value);

            return await query
                .SelectMany(o => o.Items)
                .Where(oi => oi.Product!.ProductStores.Any(ps => ps.StoreId == storeId && !ps.IsDeleted))
                .SumAsync(oi => (oi.Quantity * oi.UnitPrice) , cancellationToken);
        }

        //  Queryable access for complex queries
        public IQueryable<Order> GetQueryable()
        {
            return _context.Orders.AsNoTracking();
        }

    }
}
