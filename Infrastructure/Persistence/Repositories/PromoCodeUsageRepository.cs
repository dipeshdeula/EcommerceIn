using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class PromoCodeUsageRepository : Repository<PromoCodeUsage> , IPromocodeUsageRepository
    {
        private readonly MainDbContext _context;
        public PromoCodeUsageRepository(MainDbContext context) : base(context)
        {
            _context = context;
            
        }
    }
}
