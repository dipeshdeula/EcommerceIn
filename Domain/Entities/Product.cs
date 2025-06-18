using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Product
    {
        
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MarketPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public int ReservedStock { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public int Reviews { get; set; }
        public decimal Rating { get; set; }
        public string? Dimensions { get; set; }
        public bool IsDeleted { get; set; } = false;
        public uint Version { get; set; }

        //  FOREIGN KEYS 
        public int SubSubCategoryId { get; set; }
        public int CategoryId { get; set; }

        //  NAVIGATION PROPERTIES 
        public SubSubCategory? SubSubCategory { get; set; }
        public Category? Category { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
        public ICollection<EventProduct> EventProducts { get; set; } = new List<EventProduct>();

        //  ESSENTIAL DOMAIN COMPUTED PROPERTIES 
        [NotMapped]
        public int AvailableStock => Math.Max(0, StockQuantity - ReservedStock);

        [NotMapped]
        public bool IsInStock => AvailableStock > 0;

        [NotMapped]
        public bool HasProductDiscount => DiscountPrice.HasValue && DiscountPrice < MarketPrice;

        [NotMapped]
        public decimal BasePrice => DiscountPrice ?? MarketPrice;

        //  DOMAIN BUSINESS METHODS 
        public bool CanReserve(int quantity) =>
            quantity > 0 && !IsDeleted && AvailableStock >= quantity;

        public bool TryReserveStock(int quantity)
        {
            if (!CanReserve(quantity)) return false;
            ReservedStock += quantity;
            return true;
        }

        public bool TryReleaseStock(int quantity)
        {
            if (quantity <= 0 || ReservedStock < quantity) return false;
            ReservedStock = Math.Max(0, ReservedStock - quantity);
            return true;
        }

        public void AddStock(int quantity)
        {
            if (quantity > 0) StockQuantity += quantity;
        }

        public void ApplyProductDiscount(decimal discountPercentage)
        {
            if (discountPercentage > 0 && discountPercentage <= 100)
            {
                var discountAmount = (MarketPrice * discountPercentage) / 100;
                DiscountPrice = Math.Max(0, MarketPrice - discountAmount);
            }
        }
    }

}
