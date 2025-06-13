using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category> GetByNameAsync(string name);
        Task<Category> GetBySlugAsync(string slug);
        Task<IEnumerable<SubCategory>> GetSubCategoriesByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(
        int categoryId,
        int skip,
        int take,
        string orderBy = "Id");

        

        Task SoftDeleteCategoryAsync(int categoryId, CancellationToken cancellationToken);
        Task HardDeleteCategoryAsync(int categoryId, CancellationToken cancellationToken);
        Task<bool> UndeleteCategoryAsync(int categoryId, CancellationToken cancellationToken);
            

        // Task<Category> GetByIdWithProductsAsync(int id);
        /* Task<IEnumerable<Category>> GetAllWithProductsAsync();
         Task<IEnumerable<Category>> GetAllWithSubCategoriesAsync();
         Task<IEnumerable<Category>> GetAllWithParentCategoryAsync();*/
    }
}
