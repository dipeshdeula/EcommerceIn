using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Interfaces.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetProductsWithCategoryAsync(
        int? categoryId = null,
        int skip = 0,
        int take = 20);

        Task<IEnumerable<Product>> GetProductsByCategoryAsync(
            int categoryId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
            
            

        Task<IEnumerable<Product>> GetProductsWithImagesAsync(
            Expression<Func<Product, bool>> predicate = null);

        Task<Product> GetProductWithFullDetailsAsync(int productId, CancellationToken cancellationToken=default);

        // Search functionality
        Task<IEnumerable<Product>> SearchProductsAsync(
            string searchTerm,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int skip = 0,
            int take = 20);

        // Bulk Operations
        Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds,CancellationToken cancellationToken = default);       
        Task<List<int>> GetExistingProductIdsAsync(List<int> productIds, CancellationToken cancellationToken = default);
        Task<bool> AllProductsExistAsync(List<int> productIds, CancellationToken cancellationToken = default);

        // LifeCycle Operation

        Task ReloadAsync(Product product);
        Task SoftDeleteProductAsync(int productId, CancellationToken cancellationToken);
        Task HardDeleteProductAsync(int productId, CancellationToken cancellationToken = default);
        Task<bool> UndeleteProductAsync(int productId, CancellationToken cancellationToken = default);



    }
}
