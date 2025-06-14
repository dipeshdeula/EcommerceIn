using Application.Extension;
using Application.Interfaces.Repositories;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly MainDbContext _context;

        public CategoryRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }

        // Get category by name
        public async Task<Category> GetByNameAsync(string name)
        {
            return await _context.Categories
                .AsNoTracking() // Read-only query
                .Include(c => c.SubCategories) // Include subcategories
                .FirstOrDefaultAsync(c => c.Name == name);
        }

        // Get category by slug
        public async Task<Category> GetBySlugAsync(string slug)
        {
            return await _context.Categories
                .AsNoTracking() // Read-only query
                .Include(c => c.SubCategories) // Include subcategories
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        // Find category by ID with related entities
        public override async Task<Category> FindByIdAsync(object id)
        {
            return await _context.Categories
                .Include(c => c.SubCategories) // Include subcategories
                    .ThenInclude(sc => sc.SubSubCategories) // Include sub-subcategories
                        .ThenInclude(ssc => ssc.Products) // Include products in sub-subcategories
                .FirstOrDefaultAsync(c => c.Id == (int)id);
        }

        // Get all categories with optional filters
        public override async Task<IEnumerable<Category>> GetAllAsync(
            Expression<Func<Category, bool>> predicate = null,
            Func<IQueryable<Category>, IOrderedQueryable<Category>> orderBy = null,
            int? skip = null,
            int? take = null,
            bool includeDeleted = false,
            string includeProperties = null
            )
        {
            var query = _context.Categories
                .AsNoTracking() // Read-only query                
                .Include(c => c.SubCategories) // Include subcategories
                    .ThenInclude(sc => sc.SubSubCategories) // Include sub-subcategories
                        .ThenInclude(ssc => ssc.Products) // Include products in sub-subcategories
                .AsQueryable();

            // Filter out deleted category if includeDeleted is false
            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        // Get subcategories for a specific category
        public async Task<IEnumerable<SubCategory>> GetSubCategoriesByCategoryIdAsync(int categoryId)
        {
            return await _context.SubCategories
                .AsNoTracking() // Read-only query
                .Where(sc => sc.CategoryId == categoryId)
                .Include(sc => sc.SubSubCategories) // Include sub-subcategories
                .ToListAsync();
        }

        // Get sub-subcategories for a specific subcategory
        public async Task<IEnumerable<SubSubCategory>> GetSubSubCategoriesBySubCategoryIdAsync(int subCategoryId)
        {
            return await _context.SubSubCategories
                .AsNoTracking() // Read-only query
                .Where(ssc => ssc.SubCategoryId == subCategoryId)
                .Include(ssc => ssc.Products) // Include products
                .ToListAsync();
        }

        // Get products for a specific sub-subcategory
        public async Task<IEnumerable<Product>> GetProductsBySubSubCategoryIdAsync(int subSubCategoryId,int skip,int take)
        {
            return await _context.Products
                .AsNoTracking() // Read-only query
                .Where(p => p.SubSubCategoryId == subSubCategoryId)
                .Include(p => p.Images) // Include product images
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(
            int categoryId,
            int skip, int take,
            string orderBy = "Id")
        {
            return await _context.Products
        .Where(p => p.CategoryId == categoryId)
        .Include(p=>p.Images)
        .OrderBy(p => EF.Property<object>(p, orderBy)) // Sort by ProductId
        .Skip(skip)
        .Take(take)
        .ToListAsync();
        }

        public async Task SoftDeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category != null)
            {
                // Soft delete the category
                await _context.SoftDeleteAsync(category, cancellationToken);
            }
        }

        public async Task HardDeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category != null)
            {
                 await _context.HardDeleteAsync(category, cancellationToken);
            }

        }

        public async Task<bool> UndeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category != null)
            {
                return await _context.UndeleteAsync(category, cancellationToken);
            }
            return false;
        }
    }
}
