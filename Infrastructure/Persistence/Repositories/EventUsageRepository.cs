using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class EventUsageRepository : Repository<EventUsage>, IEventUsageRepository
    {
        private readonly MainDbContext _context;
        public EventUsageRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EventUsage>> GetByEventIdAsync(int eventId)
        {
            return await _context.EventUsages
               .Where(eu => eu.BannerEventId == eventId && !eu.IsDeleted)
               .Include(eu => eu.User)
               .OrderByDescending(eu => eu.UsedAt)
               .ToListAsync();
        }

        public async Task<EventUsage?> GetByOrderIdAsync(int orderId)
        {
            return await _context.EventUsages
                .Include(eu => eu.BannerEvent)
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.OrderId == orderId && !eu.IsDeleted);
        }

        public async Task<IEnumerable<EventUsage>> GetByUserIdAsync(int userId)
        {
            return await _context.EventUsages
               .Where(eu => eu.UserId == userId && !eu.IsDeleted)
               .Include(eu => eu.BannerEvent)
               .OrderByDescending(eu => eu.UsedAt)
               .ToListAsync();
        }

        public async Task<int> GetUsageCountByEventAndUserAsync(int eventId, int userId)
        {
            return await _context.EventUsages
                .CountAsync(eu => eu.BannerEventId == eventId &&
                                 eu.UserId == userId && eu.IsActive &&
                                 !eu.IsDeleted);
        }

         public async Task<IEnumerable<EventUsage>> GetUsagesByDateRangeAsync(int eventId, DateTime fromDate, DateTime toDate)
        {
            return await _context.EventUsages
                .Where(eu => eu.BannerEventId == eventId &&
                           eu.UsedAt >= fromDate &&
                           eu.UsedAt <= toDate &&
                           !eu.IsDeleted)
                .Include(eu => eu.User)
                .Include(eu => eu.BannerEvent)
                .OrderByDescending(eu => eu.UsedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalDiscountByEventAsync(int eventId)
        {
            return await _context.EventUsages
                .Where(eu => eu.BannerEventId == eventId && !eu.IsDeleted)
                .SumAsync(eu => eu.DiscountApplied);
        }

        public async Task<int> GetUniqueUsersCountByEventAsync(int eventId)
        {
            return await _context.EventUsages
                .Where(eu => eu.BannerEventId == eventId && !eu.IsDeleted)
                .Select(eu => eu.UserId)
                .Distinct()
                .CountAsync();
        }
    }
}
