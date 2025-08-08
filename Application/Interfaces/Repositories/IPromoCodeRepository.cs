using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IPromoCodeRepository : IRepository<PromoCode>
    {
        Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<List<PromoCode>> GetActivePromoCodesAsync(CancellationToken cancellationToken = default);
        Task<List<PromoCodeUsage>> GetUserUsageHistoryAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> HasUserUsedCodeAsync(int userId, int promoCodeId, CancellationToken cancellationToken = default);
        Task IncrementUsageCountAsync(int promoCodeId, CancellationToken cancellationToken = default);
        Task<List<PromoCode>> GetExpiringCodesAsync(int daysAhead = 7, CancellationToken cancellationToken = default);
    }
}