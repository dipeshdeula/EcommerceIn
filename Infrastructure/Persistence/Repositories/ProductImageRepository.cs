using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductImageRepository : Repository<ProductImage>,IProductImageRepository
    {
        private readonly MainDbContext _context;
        public ProductImageRepository(MainDbContext context) : base(context) 
        {
            _context = context;
            
        }
    }
}
