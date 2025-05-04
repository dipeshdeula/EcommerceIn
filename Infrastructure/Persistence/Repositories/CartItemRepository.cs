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

        public async Task LoadNavigationProperties(CartItem cartItem)
        {
            await _context.Entry(cartItem)
                .Reference(c => c.User)
                .LoadAsync();

            await _context.Entry(cartItem)
                .Reference(c => c.Product)
                .LoadAsync();
        }
    }

}
