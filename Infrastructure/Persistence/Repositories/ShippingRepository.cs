using Application.Interfaces.Repositories;

namespace Infrastructure.Persistence.Repositories
{
    public class ShippingRepository : Repository<Shipping>, IShippingRepository
    {
        private readonly MainDbContext _context;
        public ShippingRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
