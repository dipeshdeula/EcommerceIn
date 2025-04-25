using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly MainDbContext _context;
        private readonly IFileServices _fileServices;
        public ProductRepository(MainDbContext context, IFileServices fileServices) : base(context)
        {
            _context = context;
            _fileServices = fileServices;

        }

        public override async Task<IEnumerable<Product>> GetAllAsync(
     Expression<Func<Product, bool>> predicate = null,
     Func<IQueryable<Product>, IOrderedQueryable<Product>> orderBy = null,
     int? skip = null,
     int? take = null,
     bool includeDeleted = false,
        string includeProperties = null
     )
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

        public async Task SoftDeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images) // Include images for soft delete
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                // Soft delete the product
                await _context.SoftDeleteAsync(product, cancellationToken);
            }
        }

        public async Task HardDeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images) // Include images for hard delete
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                // Delete associated images from the file system
                foreach (var image in product.Images)
                {
                    await _fileServices.DeleteFileAsync(image.ImageUrl, FileType.ProductImages);
                }

                // Use the HardDeleteAsync method from DeleteExtensions
                await _context.HardDeleteAsync(product, cancellationToken);
            }
        }



        public async Task<bool> UndeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images) // Include images for undelete
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                // Undelete the product
                return await _context.UndeleteAsync(product, cancellationToken);
            }

            return false;
        }
    }
}
