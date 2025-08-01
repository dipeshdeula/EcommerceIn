﻿
using Domain.Enums.BannerEventSpecial;

namespace Application.Dto.ProductDTOs
{
    public class ProductPriceInfoDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        //  PRICING BREAKDOWN
        public decimal OriginalPrice { get; set; }        // Market price (full price)
        public decimal BasePrice { get; set; }            // Product discount price (DiscountPrice ?? MarketPrice)
        public decimal EffectivePrice { get; set; }       // Final price after all discounts

        public decimal ProductDiscountAmount { get; set; } // Regular discount amount - SET by SERVICE


        public decimal RegularDiscountAmount => ProductDiscountAmount;
        public decimal EventDiscountAmount { get; set; }    // Event discount amount
        public decimal TotalDiscountAmount => ProductDiscountAmount + EventDiscountAmount; // Combined savings

        // Calculated properties with proper validation
        public decimal DiscountAmount => Math.Max(0, OriginalPrice - EffectivePrice); // Total discount (for backward compatibility)
        public decimal TotalDiscountPercentage => OriginalPrice > 0 ? Math.Round((TotalDiscountAmount / OriginalPrice) * 100, 2) : 0;

        // Discount flags (set by service)
        public bool HasProductDiscount { get; set; } = false; // Indicates if product has any discount
        public bool HasEventDiscount { get; set; } = false; // Indicates if event discount is applied

        // computed flags (Based on amounts)
        public bool HasDiscount => TotalDiscountAmount > 0;
        public bool HasRegularDiscount => ProductDiscountAmount > 0;
        public bool HasAnyDiscount => HasDiscount;
        public bool IsOnSale { get; set; } = false; // Alias for UI convenience

        

        //  EVENT INFORMATION
        public int? AppliedEventId { get; set; }
        public string? AppliedEventName { get; set; }
        public int? ActiveEventId { get; set; } 
        public string? ActiveEventName { get; set; } 
        
        public string? EventTagLine { get; set; }
        public PromotionType? PromotionType { get; set; }
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }

        //  Event status helpers
         public bool HasActiveEvent { get; set; }
        public bool IsEventActive { get; set; }
        public TimeSpan? EventTimeRemaining { get; set; }
        public bool IsEventExpiringSoon { get; set; }
        public string EventStatus { get; set; } = "";

        // Special PROMOTIONS
        public bool HasFreeShipping { get; set; } = false;
        
        // FORMATTED DISPLAY STRINGS
        public string FormattedOriginalPrice => $"Rs.{OriginalPrice:F2}";
        public string FormattedBasePrice => $"Rs.{BasePrice:F2}";
        public string FormattedEffectivePrice => $"Rs.{EffectivePrice:F2}";
        public string FormattedSavings { get; set; } = "";
        public string FormattedDiscountBreakdown { get; set; } = "";

        // CART SPECIFIC PROPERTIES : STOCK MANAGEMENT
        public bool IsStockAvailable { get; set; } = true;
        public int AvailableStock { get; set; }
        public bool CanReserveStock { get; set; } = true;
        public string? StockMessage { get; set; }

        //  PRICE VALIDATION FOR CART
        public bool IsPriceStable { get; set; } = true;
        public DateTime PriceCalculatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow; // Alias
        public bool IsPriceStable1 { get; set; } = true; // for compatibility
        public bool IsValidForCart => IsStockAvailable && CanReserveStock && IsPriceStable;

        // Backward compatibliy aliases
        public decimal BaseDiscountAmount => ProductDiscountAmount;
        public decimal DiscountPercentage => TotalDiscountPercentage;

        // Price validation with tolerance
        public bool IsPriceValidWithTolerance(decimal expectedPrice, decimal tolerance = 0.01m)
        {
            return Math.Abs(EffectivePrice - expectedPrice) <= tolerance;
        }

        //  Cart-friendly methods
        public ProductPriceInfoDTO WithStockInfo(int availableStock, bool canReserve = true)
        {
            AvailableStock = availableStock;
            IsStockAvailable = availableStock > 0;
            CanReserveStock = canReserve && IsStockAvailable;
            StockMessage = GetStockMessage(availableStock);
            return this;
        }

        public ProductPriceInfoDTO WithEventInfo(int? eventId, string? eventName, string? tagLine = null, DateTime? endDate = null)
        {
            AppliedEventId = eventId;
            ActiveEventId = eventId;
            ActiveEventName = eventName;
            AppliedEventName = eventName;
            EventTagLine = tagLine;
            EventEndDate = endDate;
            return this;
        }

        public ProductPriceInfoDTO WithPriceValidation(bool isStable = true, DateTime? calculatedAt = null)
        {
            IsPriceStable = isStable;
            IsPriceStable1 = isStable; // for compatibility set alias
            PriceCalculatedAt = calculatedAt ?? DateTime.UtcNow;
            CalculatedAt = PriceCalculatedAt; // Alias for backward compatibility
            return this;
        }

        // Helper methods for display
        private string GetFormattedSavings()
        {
            if (!HasDiscount) return string.Empty;

            var parts = new List<string>();

            if (HasRegularDiscount)
                parts.Add($"Rs.{RegularDiscountAmount:F2} regular");

            if (HasEventDiscount)
                parts.Add($"Rs.{EventDiscountAmount:F2} event");

            var breakdown = parts.Any() ? $" ({string.Join(" + ", parts)})" : "";
            return $"Save Rs.{TotalDiscountAmount:F2}{breakdown}";
        }

        private string GetStockMessage(int availableStock)
        {
            return availableStock switch
            {
                <= 0 => "Out of Stock",
                <= 5 => "Only few left!",
                <= 10 => "Low Stock",
                _ => "In Stock"
            };
        }

        //  Discount breakdown for complex scenarios
        public DiscountBreakdown GetDiscountBreakdown()
        {
            return new DiscountBreakdown
            {
                OriginalPrice = OriginalPrice,
                BasePrice = BasePrice,
                FinalPrice = EffectivePrice,
                RegularDiscount = ProductDiscountAmount,
                EventDiscount = EventDiscountAmount,
                TotalSavings = TotalDiscountAmount,
                HasRegularDiscount = HasRegularDiscount,
                HasEventDiscount = HasEventDiscount,
                DiscountPercentage = TotalDiscountPercentage,
                FormattedBreakdown = GetFormattedSavings()
            };
        }

        // Validation method for cart operations
        public CartValidationResult ValidateForCartOperation(int requestedQuantity, decimal? maxAcceptablePrice = null)
        {
            var errors = new List<string>();

            if (!IsStockAvailable)
                errors.Add("Product is out of stock");
            else if (AvailableStock < requestedQuantity)
                errors.Add($"Insufficient stock. Available: {AvailableStock}, Requested: {requestedQuantity}");

            if (!CanReserveStock)
                errors.Add("Cannot reserve stock for this product");

            if (!IsPriceStable)
                errors.Add("Price has changed since last calculation");

            if (maxAcceptablePrice.HasValue && EffectivePrice > maxAcceptablePrice.Value)
                errors.Add($"Price exceeds maximum acceptable amount. Current: Rs.{EffectivePrice:F2}, Max: Rs.{maxAcceptablePrice:F2}");

            return new CartValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                AvailableStock = AvailableStock,
                CurrentPrice = EffectivePrice,
                CanProceed = IsValidForCart && !errors.Any()
            };
        }
    }

    // SUPPORTING CLASSES
    public class DiscountBreakdown
    {
        public decimal OriginalPrice { get; set; }
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal RegularDiscount { get; set; }
        public decimal EventDiscount { get; set; }
        public decimal TotalSavings { get; set; }
        public bool HasRegularDiscount { get; set; }
        public bool HasEventDiscount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string FormattedBreakdown { get; set; } = string.Empty;
    }

    public class CartValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int AvailableStock { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool CanProceed { get; set; }
        public string ErrorMessage => string.Join("; ", Errors);
    }
    
    
}
