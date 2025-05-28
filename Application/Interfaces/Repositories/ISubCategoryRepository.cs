using Application.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface ISubCategoryRepository : IRepository<SubCategory>
    {
        Task<SubCategory> GetByIdAsync(int id);
        Task<IEnumerable<SubSubCategory>> GetSubSubCategoriesBySubCategoryIdAsync();
        Task SoftDeleteSubCategoryAsync(int categoryId, CancellationToken cancellationToken);
        Task HardDeleteSubCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
        Task<bool> UndeleteSubCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    }
}
