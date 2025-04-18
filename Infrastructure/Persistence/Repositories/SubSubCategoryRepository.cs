using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class SubSubCategoryRepository : Repository<SubSubCategory>,ISubSubCategoryRepository
    {
        private readonly MainDbContext _context;
        public SubSubCategoryRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
       
    }
}
