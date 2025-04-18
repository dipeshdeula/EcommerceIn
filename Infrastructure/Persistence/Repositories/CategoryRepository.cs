using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
            bool includeDeleted = false)
        {
            var query = _context.Categories
                .AsNoTracking() // Read-only query
                .Include(c => c.SubCategories) // Include subcategories
                    .ThenInclude(sc => sc.SubSubCategories) // Include sub-subcategories
                        .ThenInclude(ssc => ssc.Products) // Include products in sub-subcategories
                .AsQueryable();

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
        public async Task<IEnumerable<Product>> GetProductsBySubSubCategoryIdAsync(int subSubCategoryId)
        {
            return await _context.Products
                .AsNoTracking() // Read-only query
                .Where(p => p.SubSubCategoryId == subSubCategoryId)
                .Include(p => p.Images) // Include product images
                .ToListAsync();
        }
    }
}
