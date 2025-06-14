using Application.Interfaces.Repositories;
using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class BannerEventSpecialRepository : Repository<BannerEventSpecial>,IBannerEventSpecialRepository
    {
        private readonly MainDbContext _context;
        public BannerEventSpecialRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BannerEventSpecial>> GetExpiredEventsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.BannerEventSpecials
                .Where(e => e.IsActive &&
                           !e.IsDeleted &&
                           e.Status == EventStatus.Active &&
                           e.EndDate < now)
                .Include(e => e.EventProducts)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<BannerEventSpecial>> GetActiveEventsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.BannerEventSpecials
                .Where(e => e.IsActive &&
                           !e.IsDeleted &&
                           e.Status == EventStatus.Active &&
                           e.StartDate <= now &&
                           e.EndDate >= now &&
                           e.CurrentUsageCount < e.MaxUsageCount)
                .Include(e => e.EventProducts)
                .OrderByDescending(e => e.Priority)
                .ToListAsync(cancellationToken);
        }

        public async Task<BannerEventSpecial?> GetActiveEventForProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.BannerEventSpecials
                .Where(e => e.IsActive &&
                           !e.IsDeleted &&
                           e.Status == EventStatus.Active &&
                           e.StartDate <= now &&
                           e.EndDate >= now &&
                           e.CurrentUsageCount < e.MaxUsageCount &&
                           (!e.EventProducts.Any() || e.EventProducts.Any(ep => ep.ProductId == productId)))
                .Include(e => e.EventProducts)
                .OrderByDescending(e => e.Priority)
                .ThenByDescending(e => e.DiscountValue)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
