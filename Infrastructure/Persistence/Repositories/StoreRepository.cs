using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class StoreRepository : Repository<Store>,IStoreRepository
    {
        private readonly MainDbContext _context;
        public StoreRepository(MainDbContext context) :base(context)
        {
            _context = context;
            
        }
    }
}
