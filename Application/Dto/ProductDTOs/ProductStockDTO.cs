using Application.Dto.Shared;
using Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class ProductStockDTO
    {
        public int ProductId { get; set; }

        // ===== STOCK INFORMATION =====
        public int TotalStock { get; set; }
        public int ReservedStock { get; set; }
        public int AvailableStock => Math.Max(0, TotalStock - ReservedStock);

        // ===== STOCK STATUS =====
        public bool IsInStock => AvailableStock > 0;
        public bool IsOutOfStock => AvailableStock <= 0;
        public bool IsLowStock => AvailableStock > 0 && AvailableStock <= 10;
        public StockLevel StockLevel => GetStockLevel();

        // ===== CART CAPABILITIES =====
        public bool CanReserve { get; set; } = true;
        public bool IsAvailableForSale { get; set; } = true;
        public int MaxOrderQuantity { get; set; } = int.MaxValue;

        // ===== STOCK MESSAGES =====
        public string StockStatus => IsOutOfStock ? "Out of Stock"
                                   : IsLowStock ? "Only few left!"
                                   : "In Stock";

        public string StockMessage => IsOutOfStock ? "Currently unavailable"
                                    : IsLowStock ? $"Only {AvailableStock} left in stock"
                                    : $"{AvailableStock} in stock";

        // ===== VALIDATION METHODS =====
        public bool CanAddToCart(int quantity = 1) =>
            IsAvailableForSale && IsInStock && CanReserve &&
            quantity > 0 && quantity <= AvailableStock &&
            quantity <= MaxOrderQuantity;

        public StockValidationResult ValidateQuantity(int requestedQuantity)
        {
            var errors = new List<string>();

            if (!IsAvailableForSale)
                errors.Add("Product is not available for sale");

            if (requestedQuantity <= 0)
                errors.Add("Quantity must be greater than 0");

            if (IsOutOfStock)
                errors.Add("Product is out of stock");
            else if (requestedQuantity > AvailableStock)
                errors.Add($"Only {AvailableStock} available, requested {requestedQuantity}");

            if (requestedQuantity > MaxOrderQuantity)
                errors.Add($"Maximum order quantity is {MaxOrderQuantity}");

            if (!CanReserve)
                errors.Add("Cannot reserve stock for this product");

            return new StockValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                AvailableQuantity = AvailableStock,
                MaxAllowedQuantity = Math.Min(AvailableStock, MaxOrderQuantity),
                CanProceed = CanAddToCart(requestedQuantity)
            };
        }

        // ===== HELPER METHODS =====
        private StockLevel GetStockLevel()
        {
            if (IsOutOfStock) return StockLevel.OutOfStock;
            if (IsLowStock) return StockLevel.Low;
            if (AvailableStock <= 50) return StockLevel.Medium;
            return StockLevel.High;
        }
    }
}
