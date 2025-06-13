using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface ISubSubCategoryRepository : IRepository<SubSubCategory>
    {
        Task<SubSubCategory> FindByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsBySubSubCategoryIdAsync(
            int subSubCategoryId,           
            int skip,
            int take          
            );

        Task SoftDeleteSubSubCategoryAsync(int subSubcategoryId, CancellationToken cancellationToken);
        Task HardDeleteSubSubCategoryAsync(int subSubcategoryId, CancellationToken cancellationToken);
        Task<bool> UndeleteSubSubCategoryAsync(int subSubcategoryId, CancellationToken cancellationToken);
    }
}
