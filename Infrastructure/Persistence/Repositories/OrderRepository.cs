using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class OrderRepository : Repository<Order>,IOrderRepository
    {
        private readonly MainDbContext _context;

        public OrderRepository(MainDbContext dbcontext) : base(dbcontext)
        {
            _context = dbcontext;
            
        }

    }
}
