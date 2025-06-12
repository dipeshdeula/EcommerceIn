using Application.Dto.ProductDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Shared
{
    public class ProductDetailsDTO
    {
        public ProductDTO Product { get; set; } = new();
        public ProductPriceInfoDTO Pricing { get; set; } = new();
        public ProductStockDTO Stock { get; set; } = new();

        //  CONVENIENCE PROPERTIES 
        public bool IsAvailableForPurchase => Product.IsInStock && Stock.CanAddToCart() && Pricing.IsPriceStable;
        public bool HasDiscount => Pricing.HasDiscount;
        public string DisplayPrice => Pricing.FormattedEffectivePrice;
        public string DisplaySavings => Pricing.FormattedSavings;

        //  CART VALIDATION 
        public CartValidationResult ValidateForCart(int quantity, decimal? maxAcceptablePrice = null)
        {
            var errors = new List<string>();

            // Stock validation
            var stockValidation = Stock.ValidateQuantity(quantity);
            if (!stockValidation.IsValid)
                errors.AddRange(stockValidation.Errors);

            // Price validation
            if (!Pricing.IsPriceStable)
                errors.Add("Price has changed since last calculation");

            if (maxAcceptablePrice.HasValue && Pricing.EffectivePrice > maxAcceptablePrice.Value)
                errors.Add($"Price exceeds maximum acceptable amount");

            return new CartValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                CurrentPrice = Pricing.EffectivePrice,
                AvailableStock = Stock.AvailableStock,
                CanProceed = IsAvailableForPurchase && !errors.Any()
            };
        }
    }
}
