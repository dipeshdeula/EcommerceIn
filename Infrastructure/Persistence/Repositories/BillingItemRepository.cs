using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class BillingItemRepository : Repository<BillingItem>,IBillingItemRepository
    {
        private readonly MainDbContext _context;
        public BillingItemRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
