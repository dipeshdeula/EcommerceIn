using Application.Enums;
using Application.Extension;
using Application.Interfaces.Repositories;
using System.Linq.Expressions;
using System.Threading;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly MainDbContext _context;
        private readonly IFileServices _fileServices;
        public ProductRepository(
            MainDbContext context,
            IFileServices fileServices
            ) : base(context)
        {
            _context = context;
            _fileServices = fileServices;
        }

        public async Task ReloadAsync(Product product)
        {
            await _context.Entry(product).ReloadAsync();
        }


        public override async Task<IEnumerable<Product>> GetAllAsync(
             Expression<Func<Product, bool>> predicate = null,
             Func<IQueryable<Product>, IOrderedQueryable<Product>> orderBy = null,
             int? skip = null,
             int? take = null,
             bool includeDeleted = false,
             string includeProperties = null)
        {
            //  Start with base queryable, not specific DbSet
            IQueryable<Product> query = GetQueryable(includeDeleted);

            // Apply predicate first for better performance
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            //  Handle include properties properly
            if (!string.IsNullOrEmpty(includeProperties))
            {
                var properties = includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var property in properties)
                {
                    var trimmedProperty = property.Trim();
                    query = query.Include(trimmedProperty);
                }
            }
            else
            {
                // Default includes for Product
                query = query
                    .Include(p => p.Images)
                    .Include(p => p.Category)
                    .Include(p => p.SubSubCategory);
            }

            // Apply ordering
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Apply pagination
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



        public async Task<IEnumerable<Product>> GetProductsWithCategoryAsync(int? categoryId = null, int skip = 0, int take = 20)
        {
            IQueryable<Product> query = _context.Products.Where(p => !p.IsDeleted);

            // Apply category filter before includes to optimize query
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Apply includes after filtering
            query = query.Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.SubSubCategory);

            // Apply pagination
            var products = await query.Skip(skip).Take(take).ToListAsync();

            return products;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId, bool includeDeleted = false,CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(
                 predicate: p => p.CategoryId == categoryId && (includeDeleted || !p.IsDeleted),
                 includeProperties: "Images,Category",
                cancellationToken: cancellationToken

                 );
        }
        
        public async Task<IEnumerable<Product>> GetProductsWithImagesAsync(Expression<Func<Product, bool>> predicate = null)
        {
            return await GetWithIncludesAsync(
               predicate: predicate,
               includes: p => p.Images);
        }

        public async Task<Product> GetProductWithFullDetailsAsync(int productId,CancellationToken cancellationToken)
        {
            var products = await GetAllAsync(
                predicate: p => p.Id == productId && !p.IsDeleted,
                includeProperties: "Images,Category,SubSubCategory",
                cancellationToken:cancellationToken
                );

            return products.FirstOrDefault();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, int? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, int skip = 0, int take = 20)
        {
            IQueryable<Product> query = GetQueryable(false);

            // Search by name or description
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) ||
                                        p.Description.Contains(searchTerm));
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by price range
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.MarketPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.MarketPrice <= maxPrice.Value);
            }

            // Include related data
            query = query
                .Include(p => p.Images)
                .Include(p => p.Category);

            // Apply pagination and ordering
            return await query
                .OrderBy(p => p.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
        

        public async Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds)
        {
            return await GetWithIncludesAsync(
               predicate: p => productIds.Contains(p.Id) && !p.IsDeleted,
               includes: p => p.Images
             
               );
        }

        public async Task<List<int>> GetExistingProductIdsAsync(List<int> productIds, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                 .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                 .Select(p => p.Id)
                 .ToListAsync(cancellationToken);
        }

        public async Task<bool> AllProductsExistAsync(List<int> productIds, CancellationToken cancellationToken = default)
        {
            var existingCount = await _context.Products
                .CountAsync(p => productIds.Contains(p.Id) && !p.IsDeleted, cancellationToken);

            return existingCount == productIds.Distinct().Count();
        }

        public async Task<int> CountAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Products.Where(predicate).CountAsync(cancellationToken);
        }


        public async Task SoftDeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            /*var product = await _context.Products
                .Include(p => p.Images) // Include images for soft delete
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                // Soft delete the product
                await _context.SoftDeleteAsync(product, cancellationToken);
            }*/

            var product = await FindByIdAsync(productId);
            if (product != null)
            {
                //  Use repository's soft delete method
                await SoftDeleteAsync(product, cancellationToken);
                
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

            /* var products = await GetWithIncludesAsync(
                predicate: p => p.Id == productId,
                includes: p => p.Images);

            var product = products.FirstOrDefault();
            if (product != null)
            {
                // Delete associated images from the file system
                if (product.Images?.Any() == true)
                {
                    foreach (var image in product.Images)
                    {
                        try
                        {
                            await _fileServices.DeleteFileAsync(image.ImageUrl, FileType.ProductImages);
                        }
                        catch (Exception ex)
                        {
                            // Log the error but continue with product deletion
                            System.Diagnostics.Debug.WriteLine($"Failed to delete image {image.ImageUrl}: {ex.Message}");
                        }
                    }
                }

                //  Use repository's remove method
                Remove(product);
            }
             
             */
        }

  

        public Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
