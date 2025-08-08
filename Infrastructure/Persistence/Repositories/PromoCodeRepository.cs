using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class PromoCodeRepository : Repository<PromoCode>, IPromoCodeRepository
    {
        private readonly MainDbContext _context;
        
        public PromoCodeRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodes
                .Include(p => p.PromoCodeUsages)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower() && !p.IsDeleted, cancellationToken);
        }
        
        public async Task<List<PromoCode>> GetActivePromoCodesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.PromoCodes
                .Where(p => p.IsActive && 
                           !p.IsDeleted && 
                           p.StartDate <= now && 
                           p.EndDate >= now)
                .ToListAsync(cancellationToken);
        }
        
        public async Task<List<PromoCodeUsage>> GetUserUsageHistoryAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodeUsages
                .Include(u => u.PromoCode)
                .Where(u => u.UserId == userId && !u.IsDeleted)
                .OrderByDescending(u => u.UsedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PromoCodeUsage>> GetPromoCodeUsageHistoryAsync(int promoCodeId, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodeUsages
                .Include(u => u.User)
                .Include(u => u.Order)
                .Where(u => u.PromoCodeId == promoCodeId && !u.IsDeleted)
                .OrderByDescending(u => u.UsedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasUserUsedCodeAsync(int userId, int promoCodeId, CancellationToken cancellationToken = default)
        {
            return await _context.PromoCodeUsages
                .AnyAsync(u => u.UserId == userId && u.PromoCodeId == promoCodeId && !u.IsDeleted, cancellationToken);
        }
        
        public async Task IncrementUsageCountAsync(int promoCodeId, CancellationToken cancellationToken = default)
        {
            var promoCode = await _context.PromoCodes.FindAsync(promoCodeId);
            if (promoCode != null)
            {
                promoCode.CurrentUsageCount++;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        
        public async Task<List<PromoCode>> GetExpiringCodesAsync(int daysAhead = 7, CancellationToken cancellationToken = default)
        {
            var futureDate = DateTime.UtcNow.AddDays(daysAhead);
            return await _context.PromoCodes
                .Where(p => p.IsActive && 
                           !p.IsDeleted && 
                           p.EndDate <= futureDate && 
                           p.EndDate >= DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }
    }
}