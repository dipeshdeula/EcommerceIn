using Application.Extension;
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
        public async Task<SubSubCategory> FindByIdAsync(int id)
        {
            return await _context.SubSubCategories
                .Include(ssc => ssc.SubCategory) // Include SubCategory
                .ThenInclude(sc => sc.Category) // Include Category
                .FirstOrDefaultAsync(ssc => ssc.Id == id);
        }

        public async Task HardDeleteSubSubCategoryAsync(int subSubcategoryId, CancellationToken cancellationToken)
        {
            var subSubCategory = await _context.SubSubCategories
                .Include(ssc=>ssc.Products)
                .FirstOrDefaultAsync(ssc=>ssc.Id == subSubcategoryId,cancellationToken);

            if (subSubCategory != null)
            { 
                await _context.HardDeleteAsync(subSubCategory,cancellationToken);
            }
        }

        public async Task SoftDeleteSubSubCategoryAsync(int subSubcategoryId, CancellationToken cancellationToken)
        {
            var subSubCategory = await _context.SubSubCategories
                 .Include(ssc => ssc.Products)
                 .FirstOrDefaultAsync(ssc => ssc.Id == subSubcategoryId, cancellationToken);

            if (subSubCategory != null)
            {
                await _context.SoftDeleteAsync(subSubCategory, cancellationToken);
            }
        }

        public async Task<bool> UndeleteSubSubCategoryAsync(int subSubcategoryId, CancellationToken cancellationToken)
        {
            var subSubCategory = await _context.SubSubCategories
                 .Include(ssc => ssc.Products)
                 .FirstOrDefaultAsync(ssc => ssc.Id == subSubcategoryId, cancellationToken);

            if (subSubCategory != null)
            {
               return await _context.UndeleteAsync(subSubCategory, cancellationToken);
            }
            return false;
        }
    }
}
