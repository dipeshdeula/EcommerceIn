using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ServiceAreaRepository : Repository<ServiceArea>,IServiceAreaRepository
    {
        private readonly MainDbContext _context;
        public ServiceAreaRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
