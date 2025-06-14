using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Utilities;

namespace Infrastructure.Persistence.Repositories
{
    public class ProductStoreRepository : Repository<ProductStore>, IProductStoreRepository
    {
        private readonly MainDbContext _context;
        private readonly ICurrentUserService _userService;
        public ProductStoreRepository(
            MainDbContext context,
            ICurrentUserService userService
            ) : base(context)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IEnumerable<NearbyProductDto>> GetNearbyProductsAsync(double lat, double lon, double radiusKm, int skip, int take)
        {
            try
            {
                var stores = await _context.Stores
                    .Where(s => !s.IsDeleted).Include(s => s.Address)
                     .AsNoTracking()
                    .ToListAsync();
                if (!stores.Any())
                {
                    return new List<NearbyProductDto>();
                }
                //filter stores by distance
                var nearbyStoreIds = stores.Where(s => s.Address != null
                    && GeoUtils.CalculateDistance(lat, lon, s.Address.Latitude, s.Address.Longitude) <= radiusKm)
                    .Select(s => s.Id)
                    .ToList();

                if (!nearbyStoreIds.Any())
                {
                    return new List<NearbyProductDto>();
                }

                // get products from nearby stores
                var productStores = await _context.ProductStores
            .Where(ps => nearbyStoreIds.Contains(ps.StoreId) &&
                        !ps.IsDeleted &&
                        ps.Product != null &&
                        !ps.Product.IsDeleted &&
                        ps.Product.StockQuantity > 0)
                .Include(ps => ps.Product)
                    .ThenInclude(p => p.Images.Where(img => !img.IsDeleted))
                .Include(ps => ps.Store)
                    .ThenInclude(s => s.Address)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();

                return productStores.Select(ps =>
                {
                    var distance = GeoUtils.CalculateDistance(lat, lon, ps.Store.Address.Latitude, ps.Store.Address.Longitude);

                    return new NearbyProductDto
                    {
                        ProductId = ps.Product.Id,
                        Name = ps.Product.Name ?? "Unknown Product",
                        StoreName = ps.Store.Name ?? "Unknown Store",
                        Distance = Math.Round(distance, 2),

                        // Pricing will be updated by pricing service
                        MarketPrice = ps.Product.MarketPrice,
                        CostPrice = ps.Product.CostPrice,
                        DiscountPrice = ps.Product.DiscountPrice,
                        CurrentPrice = ps.Product.MarketPrice, // Initial Value
                        EffectivePrice = ps.Product.MarketPrice,
                        StockQuantity = ps.Product.StockQuantity,

                        ImageUrl = ps.Product.Images.FirstOrDefault(img => img.IsMain && !img.IsDeleted)?.ImageUrl ?? string.Empty,
                        StoreCity = ps.Store.Address.City,

                        StoreId = ps.Store.Id,
                        //StoreAddress = $"{ps.Store.Address.Street}, {ps.Store.Address.City}, {ps.Store.Address.Province}, {ps.Store.Address.PostalCode}",
                        StoreAddress = FormatStoreAddress(ps.Store.Address),

                        
                        HasDiscount = ps.Product.DiscountPrice.HasValue && ps.Product.DiscountPrice.Value > 0,
                        //update by pricing service
                        DiscountAmount = 0,        
                        DiscountPercentage = 0,   
                        HasActiveEvent = false,   
                        ActiveEventName = null,   

                        //  Add ProductDTO for detailed access
                        ProductDTO = ps.Product.ToDTO()
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                return new List<NearbyProductDto>();
            }

        }
        private static string FormatStoreAddress(StoreAddress? address)
        {
            if (address == null) return string.Empty;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address.Street)) parts.Add(address.Street);
            if (!string.IsNullOrWhiteSpace(address.City)) parts.Add(address.City);
            if (!string.IsNullOrWhiteSpace(address.Province)) parts.Add(address.Province);

            return string.Join(", ", parts);
        }
    }
}
    
    





