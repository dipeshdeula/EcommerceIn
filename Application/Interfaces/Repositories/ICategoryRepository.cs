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
       // Task<Category> GetByIdWithProductsAsync(int id);
       /* Task<IEnumerable<Category>> GetAllWithProductsAsync();
        Task<IEnumerable<Category>> GetAllWithSubCategoriesAsync();
        Task<IEnumerable<Category>> GetAllWithParentCategoryAsync();*/
    }
}
