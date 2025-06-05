using Application.Interfaces.Repositories;
using Domain.Entities.Common;

namespace Infrastructure.Persistence.Repositories
{
    public class EventProductRepository : Repository<EventProduct>, IEventProductRepository
    {
        private readonly MainDbContext _context;
        public EventProductRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }


        public async Task<IEnumerable<EventProduct>> GetByEventIdAsync(int eventId)
        {
            return await _context.EventProducts.Where(
                ep => ep.BannerEventId == eventId && !ep.IsDeleted)
                .Include(ep => ep.Product)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventProduct>> GetByProductIdAsync(int productId)
        {
            return await _context.EventProducts.Where(
                ep => ep.ProductId == productId && ep.IsActive && !ep.IsDeleted)
                .Include(ep => ep.BannerEvent)
                .Where(ep => ep.BannerEvent.IsActive && !ep.BannerEvent.IsDeleted)
                .ToListAsync();
        }

        public async Task<EventProduct?> GetByEventAndProductIdAsync(int eventId, int productId)
        {
            return await _context.EventProducts
                .Include(ep => ep.Product)
                .Include(ep => ep.BannerEvent)
                .FirstOrDefaultAsync(ep => ep.BannerEventId == eventId &&
                    ep.ProductId == productId &&
                    !ep.IsDeleted);
        }
    }
}
