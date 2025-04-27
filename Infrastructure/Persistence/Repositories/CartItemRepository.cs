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
    }

}
