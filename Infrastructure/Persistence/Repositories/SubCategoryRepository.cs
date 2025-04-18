using Application.Interfaces.Repositories;

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
    }
}
