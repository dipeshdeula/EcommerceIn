using Application.Dto.ProductDTOs;

namespace Application.Dto.CategoryDTOs
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<SubCategoryDTO> SubCategories { get; set; } = new List<SubCategoryDTO>();

        // Statistical properties (calculated dynamically)
        public int ProductCount { get; set; } = 0;
        public int ProductsOnSale { get; set; } = 0;
        public decimal AveragePrice { get; set; } = 0;
        public decimal MinPrice { get; set; } = 0;
        public decimal MaxPrice { get; set; } = 0;
        public bool HasProductsOnSale { get; set; } = false;
        public decimal SalePercentage { get; set; } = 0;
        public string PriceRange { get; set; } = "Rs.0.00";

        // Additional computed properties
        public int TotalSubCategories => SubCategories?.Count ?? 0;
        public int TotalSubSubCategories => SubCategories?.SelectMany(sc => sc.SubSubCategories).Count() ?? 0;
        public bool HasProducts => ProductCount > 0;
        public string FormattedAveragePrice => $"Rs.{AveragePrice:F2}";
        public string SaleInfo => HasProductsOnSale 
            ? $"{ProductsOnSale} of {ProductCount} products on sale ({SalePercentage:F1}%)"
            : "No products on sale";
    }
}