using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductRepository : Repository<Product>,IProductRepository
    {
        private readonly MainDbContext _context;
        public ProductRepository(MainDbContext context):base(context)
        {
            _context = context;
            
        }
    }
}
