using Application.Interfaces.Repositories;

namespace Infrastructure.Persistence.Repositories
{
    public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
    {
        private readonly MainDbContext _context;
        public WishlistRepository(MainDbContext context) : base(context)
        {
            _context = context;

        }
        public async Task<Wishlist?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
        {
            return await _context.Wishlists
                .Include(w => w.User)
                .Include(w => w.Product)
                .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(w => w.UserId == userId &&
                                        w.ProductId == productId &&
                                        !w.IsDeleted,
                                   cancellationToken);
        }

        public async Task<IEnumerable<Wishlist>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Wishlists
                .Include(w => w.User)
                .Include(w => w.Product)
                .ThenInclude(p => p.Images)
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(int userId, int productId, CancellationToken cancellationToken = default)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId &&
                              w.ProductId == productId &&
                              !w.IsDeleted,
                         cancellationToken);
        }

        public async Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Wishlists
                .CountAsync(w => w.UserId == userId && !w.IsDeleted, cancellationToken);
        }

        public async Task RemoveByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
        {
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId &&
                                        w.ProductId == productId &&
                                        !w.IsDeleted,
                                   cancellationToken);

            if (wishlistItem != null)
            {
                wishlistItem.IsDeleted = true;
                wishlistItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ClearUserWishlistAsync(int userId, CancellationToken cancellationToken = default)
        {
            var wishlistItems = await _context.Wishlists
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var item in wishlistItems)
            {
                item.IsDeleted = true;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
