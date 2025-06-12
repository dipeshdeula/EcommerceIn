using Application.Dto.ProductDTOs;
using MediatR.NotificationPublishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CategoryDTOs
{
    public class CategoryWithProductsDTO
    {
        public int Id { get; set; } // Category ID
        public string Name { get; set; } // Category Name

        public string Slug { get; set; } // Category Slug
        public string Description { get; set; } // Category Description
        public IEnumerable<ProductDTO> Products { get; set; } // List of Products

        // Enhanced Statstics
        public int TotalProducts { get; set; }
        public int ProductsOnSale { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotalSavings { get; set; }

        // Computed properties
        public bool HasActiveDeals => ProductsOnSale > 0;
        public decimal SalePercentage => TotalProducts > 0 ? Math.Round((decimal)ProductsOnSale / TotalProducts * 100, 1) : 0;
        public string FormattedPriceRange => MinPrice != MaxPrice ? $"Rs.{MinPrice:F2} - Rs.{MaxPrice:F2}" : $"Rs.{MinPrice:F2}";
        public string FormattedTotalSavings => $"Rs.{TotalSavings:F2}";
        public bool HasProductsOnSale => ProductsOnSale > 0;
    }
}
