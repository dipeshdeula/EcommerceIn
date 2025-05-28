using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class BannerImageRepository : Repository<BannerImage>,IBannerImageRepository
    {
        private readonly MainDbContext _context;
        public BannerImageRepository(MainDbContext context) : base(context)
        {
            _context = context;
            
        }
    }
}
