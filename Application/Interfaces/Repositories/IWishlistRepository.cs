using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IWishlistRepository : IRepository<Wishlist>
    {
        Task<Wishlist?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Wishlist>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int userId, int productId, CancellationToken cancellationToken = default);
        Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task RemoveByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
        Task ClearUserWishlistAsync(int userId, CancellationToken cancellationToken = default);

    }
}
