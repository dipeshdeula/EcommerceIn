using Application.Interfaces.Repositories;

namespace Infrastructure.Persistence.Repositories
{
    public class OrderItemRepository : Repository<OrderItem>,IOrderItemRepository
    {
        private readonly MainDbContext _context;
        public OrderItemRepository(MainDbContext context) : base(context)
        {
            _context = context;
            
        }
    }
}
