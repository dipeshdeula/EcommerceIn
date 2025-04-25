using Application.Dto;
using Application.Interfaces.Repositories;
using Application.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductStoreRepository : Repository<ProductStore>, IProductStoreRepository
    {
        private readonly MainDbContext _context;
        public ProductStoreRepository(MainDbContext context) : base(context)
        {
            _context = context;
        }
      
        public async Task<IEnumerable<NearbyProductDto>> GetNearbyProductsAsync(double lat, double lon, double radiusKm, int skip , int take)
        {
            var stores = await _context.Stores.Where(s => !s.IsDeleted).Include(s=>s.Address).ToListAsync();

            var nearbyStores = stores.Where(s => GeoUtils.CalculateDistance(lat, lon, s.Address.Latitude, s.Address.Longitude) <= radiusKm)
                .Select(s => s.Id)
                .ToList();

            var productStores = await _context.ProductStores
                .Where(ps => nearbyStores.Contains(ps.StoreId) && ps.Product.StockQuantity > 0 && !ps.IsDeleted)
                .Include(ps => ps.Product)
                    .ThenInclude(p=>p.Images)
                .Include(ps => ps.Store)
                    .ThenInclude(s=>s.Address)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();

            return productStores.Select(ps=> new NearbyProductDto
            { 
                ProductId = ps.Product.Id,
                Name = ps.Product.Name,
                StoreName = ps.Store.Name,
                Distance = GeoUtils.CalculateDistance(lat, lon, ps.Store.Address.Latitude, ps.Store.Address.Longitude),
                Price = Convert.ToDouble(ps.Product.Price),
                ImageUrl = ps.Product.Images.FirstOrDefault(img => img.IsMain && !img.IsDeleted)?.ImageUrl ?? string.Empty,
                StoreCity = ps.Store.Address.City,
                StockQuantity = ps.Product.StockQuantity,

                StoreId = ps.Store.Id,
                StoreAddress = $"{ps.Store.Address.Street}, {ps.Store.Address.City}, {ps.Store.Address.Province}, {ps.Store.Address.PostalCode}",
                HasDiscount = ps.Product.DiscountPrice.HasValue && ps.Product.DiscountPrice.Value > 0,
                DiscountPrice = ps.Product.DiscountPrice
            }).ToList();
        }
    }
}
