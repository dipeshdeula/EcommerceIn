using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories
{
    public class SubCategoryRepository : Repository<SubCategory>, ISubCategoryRepository
    {
        private readonly MainDbContext _context;

        public SubCategoryRepository(MainDbContext context) : base(context)
        {
            _context = context;
            
        }

        public Task<SubCategory> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<SubSubCategory>> GetSubSubCategoriesBySubCategoryIdAsync()
        {
            throw new NotImplementedException();
        }

        public async Task HardDeleteSubCategoryAsync(int subCategoryId, CancellationToken cancellationToken = default)
        {
            var subCategory = await _context.SubCategories
                .Include(s => s.SubSubCategories)
                .FirstOrDefaultAsync(s => s.Id == subCategoryId, cancellationToken);

            if (subCategory != null)
            {
                await _context.HardDeleteAsync(subCategory, cancellationToken);
            }
            
        }

        public async Task SoftDeleteSubCategoryAsync(int subCategoryId, CancellationToken cancellationToken)
        {

            var subCategory = await _context.SubCategories
                .Include(p => p.SubSubCategories) 
                .FirstOrDefaultAsync(s => s.Id == subCategoryId, cancellationToken);

            if (subCategory != null)
            {
                // Soft delete the sub category
                await _context.SoftDeleteAsync(subCategory, cancellationToken);
            }
        }

        public async Task<bool> UndeleteSubCategoryAsync(int subCategoryId, CancellationToken cancellationToken = default)
        {
            var subCategory = await _context.SubCategories
                .Include(s => s.SubSubCategories)
                .FirstOrDefaultAsync(s => s.Id == subCategoryId, cancellationToken);

            if (subCategory != null)
            {
                return await _context.UndeleteAsync(subCategory, cancellationToken);
            }
            return false;
        }
    }
}
