using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class StoreAddressRepository : Repository<StoreAddress>,IStoreAddressRepository
    {
        private readonly MainDbContext _context;
        public StoreAddressRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
       
    }
}
