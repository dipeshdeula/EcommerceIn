using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Application.Dto.CategoryDTOs
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }  // e.g. "home-appliances"
        public string Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<SubCategoryDTO> SubCategories { get; set; } = new List<SubCategoryDTO>();

        //  Category pricing statistics
        public int ProductCount { get; set; }
        public int ProductsOnSale { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }

        //  Computed properties
        public bool HasProductsOnSale => ProductsOnSale > 0;
        public decimal SalePercentage => ProductCount > 0 ? (decimal)ProductsOnSale / ProductCount * 100 : 0;
        public string PriceRange => MinPrice == MaxPrice ? $"${MinPrice:F2}" : $"${MinPrice:F2} - ${MaxPrice:F2}";

    }
}

