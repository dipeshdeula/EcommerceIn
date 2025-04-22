using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task SoftDeleteProductAsync(int productId, CancellationToken cancellationToken);
        Task HardDeleteProductAsync(int productId, CancellationToken cancellationToken = default);
        Task<bool> UndeleteProductAsync(int productId, CancellationToken cancellationToken = default);
    }
}
