using Application.Features.ProductFeat.Queries;
using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class CartItemRepository : Repository<CartItem>, ICartItemRepository
    {
        private readonly MainDbContext _context;
        public CartItemRepository(MainDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }
        public async Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId)
        {
            return await _context.CartItems.Include(c => c.Product)
                 .Where(c => c.UserId == userId && !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow).ToListAsync();
        }


        public async Task LoadNavigationProperties(CartItem cartItem)
        {
            await _context.Entry(cartItem)
                .Reference(c => c.User)
                .LoadAsync();

            await _context.Entry(cartItem)
                .Reference(c => c.Product)                 
                .LoadAsync();
            if (cartItem.Product != null)
            {
                await _context.Entry(cartItem.Product)
                    .Collection(p => p.Images)
                    .LoadAsync();
            }
        }
        public async Task DeleteByUserIdAsync(int userId)
        {
            var cartItems = await _context.CartItems.Where(c => c.UserId == userId && !c.IsDeleted)
                .ToListAsync();
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountActiveCartItemsByEventAsync(int userId, int eventId, int productId)
        {
            var query = _context.CartItems
                .Where(c => c.UserId == userId &&
                        c.AppliedEventId == eventId &&
                        !c.IsDeleted &&
                        c.ExpiresAt > DateTime.UtcNow);

            // If productId is provided and > 0, filter by specific product
            // If productId is 0 or not provided, count all products
            if (productId > 0)
            {
                query = query.Where(c => c.ProductId == productId);
            }

            var cartItems = await query.ToListAsync();

            // Sum the quantities (each cart item can have multiple quantities)
            return cartItems.Sum(c => c.Quantity);
        }

    }

}
