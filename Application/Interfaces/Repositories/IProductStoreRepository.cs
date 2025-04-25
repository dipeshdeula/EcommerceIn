using Application.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IProductStoreRepository : IRepository<ProductStore>
    {
        Task<IEnumerable<NearbyProductDto>> GetNearbyProductsAsync(double lat, double lon, double radiusKm,int skip,int take);
    }
}
