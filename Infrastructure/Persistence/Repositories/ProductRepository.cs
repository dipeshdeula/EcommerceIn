using Application.Interfaces.Repositories;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly MainDbContext _context;
        public ProductRepository(MainDbContext context) : base(context)
        {
            _context = context;

        }

        public override async Task<IEnumerable<Product>> GetAllAsync(
     Expression<Func<Product, bool>> predicate = null,
     Func<IQueryable<Product>, IOrderedQueryable<Product>> orderBy = null,
     int? skip = null,
     int? take = null,
     bool includeDeleted = false)
        {
            // Start with the base query
            IQueryable<Product> query = _context.Products
                .Include(p => p.Images); // Eagerly load the Images navigation property

            // Filter out deleted products if includeDeleted is false
            if (!includeDeleted)
            {
                query = query.Where(p => !p.IsDeleted);
            }

            // Apply additional filtering if a predicate is provided
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering if specified
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Apply pagination if skip and take are specified
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            // Execute the query and return the results
            return await query.ToListAsync();
        }



    }
}
