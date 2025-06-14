using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IBannerEventSpecialRepository : IRepository<BannerEventSpecial>
    {
        Task<IEnumerable<BannerEventSpecial>> GetExpiredEventsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<BannerEventSpecial>> GetActiveEventsAsync(CancellationToken cancellationToken = default);
        Task<BannerEventSpecial?> GetActiveEventForProductAsync(int productId, CancellationToken cancellationToken = default);
    }
}
