using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class PromoCodeUsageRepository : Repository<PromoCodeUsage>, IPromocodeUsageRepository
    {
        private readonly MainDbContext _context;
        public PromoCodeUsageRepository(MainDbContext context) : base(context)
        {
            _context = context;

        }
        
        /// <summary>
        /// Get user usage count for specific promo code
        /// </summary>
        public async Task<int> GetUserUsageCountAsync(int userId, int promoCodeId, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodeUsages
                .CountAsync(u => u.UserId == userId && 
                               u.PromoCodeId == promoCodeId && 
                               !u.IsDeleted, 
                          cancellationToken);
        }
        
        /// <summary>
        /// Get user's usage history for a promo code
        /// </summary>
        public async Task<List<PromoCodeUsage>> GetUserUsageHistoryAsync(int userId, int promoCodeId, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodeUsages
                .Include(u => u.PromoCode)
                .Include(u => u.Order)
                .Where(u => u.UserId == userId && 
                           u.PromoCodeId == promoCodeId && 
                           !u.IsDeleted)
                .OrderByDescending(u => u.UsedAt)
                .ToListAsync(cancellationToken);
        }
        
        /// <summary>
        /// Get total usage count for promo code
        /// </summary>
        public async Task<int> GetTotalUsageCountAsync(int promoCodeId, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodeUsages
                .CountAsync(u => u.PromoCodeId == promoCodeId && !u.IsDeleted, cancellationToken);
        }
        
        /// <summary>
        /// Check if user has used promo code recently (within hours)
        /// </summary>
        public async Task<bool> HasUserUsedRecentlyAsync(int userId, int promoCodeId, int withinHours = 24, CancellationToken cancellationToken = default)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-withinHours);
            return await _context.PromoCodeUsages
                .AnyAsync(u => u.UserId == userId && 
                             u.PromoCodeId == promoCodeId && 
                             !u.IsDeleted &&
                             u.UsedAt >= cutoffTime, 
                        cancellationToken);
        }
    }
}
