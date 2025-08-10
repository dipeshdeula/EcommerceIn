using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Domain.Entities
{
    public class PromoCode
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public PromoCodeType Type { get; set; }
        
        public decimal DiscountValue { get; set; }
        
        public decimal? MaxDiscountAmount { get; set; }
        
        public decimal? MinOrderAmount { get; set; }
        
        public int? MaxTotalUsage { get; set; }
        
        public int? MaxUsagePerUser { get; set; }
        
        public int CurrentUsageCount { get; set; } = 0;
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsDeleted { get; set; } = false;
        
        public bool ApplyToShipping { get; set; } = false;
        
        public bool StackableWithEvents { get; set; } = false;
        
        public int? CategoryId { get; set; }
        
        [StringLength(50)]
        public string? CustomerTier { get; set; }
        
        [StringLength(1000)]
        public string? AdminNotes { get; set; }
        
        public int CreatedByUserId { get; set; }
        
        public int? LastModifiedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        //  NAVIGATION PROPERTIES
        public virtual Category? Category { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? LastModifiedByUser { get; set; }
        public virtual ICollection<PromoCodeUsage> PromoCodeUsages { get; set; } = new List<PromoCodeUsage>();

        //  DOMAIN METHODS - Business Logic
        
        /// <summary>
        /// Check if promo code is valid at specific UTC time
        /// </summary>
        public bool IsValidAtTime(DateTime utcTime)
        {
            return IsActive && 
                   !IsDeleted && 
                   IsStartedAtTime(utcTime) && 
                   !IsExpiredAtTime(utcTime) &&
                   HasUsageRemaining();
        }
        
        /// <summary>
        /// Check if promo code has started at specific UTC time
        /// </summary>
        public bool IsStartedAtTime(DateTime utcTime)
        {
            return StartDate <= utcTime;
        }
        
        /// <summary>
        /// Check if promo code is expired at specific UTC time
        /// </summary>
        public bool IsExpiredAtTime(DateTime utcTime)
        {
            return EndDate <= utcTime;
        }
        
        /// <summary>
        /// Check if promo code has usage remaining
        /// </summary>
        public bool HasUsageRemaining()
        {
            return !MaxTotalUsage.HasValue || CurrentUsageCount < MaxTotalUsage.Value;
        }
        
        /// <summary>
        /// Check if user can use this promo code
        /// </summary>
        public bool CanUserUse(int userId)
        {
            if (!MaxUsagePerUser.HasValue)
                return true;
                
            var userUsageCount = PromoCodeUsages.Count(u => u.UserId == userId);
            return userUsageCount < MaxUsagePerUser.Value;
        }
        
         /// <summary>
        ///  NEW: Get actual user usage count from database
        /// </summary>
        public int GetUserUsageCount(int userId)
        {
            return PromoCodeUsages.Count(u => u.UserId == userId && !u.IsDeleted);
        }
        
        /// <summary>
        /// Get remaining usage count for user
        /// </summary>
        public int? GetUserRemainingUsage(int userId)
        {
            if (!MaxUsagePerUser.HasValue)
                return null;

            var userUsageCount = PromoCodeUsages.Count(u => u.UserId == userId);
            return Math.Max(0, MaxUsagePerUser.Value - userUsageCount);
        }
        
        /// <summary>
        /// Get total remaining usage
        /// </summary>
        public int? GetTotalRemainingUsage()
        {
            if (!MaxTotalUsage.HasValue)
                return null;
                
            return Math.Max(0, MaxTotalUsage.Value - CurrentUsageCount);
        }
        
        /// <summary>
        /// Get time until start (from given UTC time)
        /// </summary>
        public TimeSpan GetTimeUntilStart(DateTime utcTime)
        {
            return StartDate > utcTime ? StartDate - utcTime : TimeSpan.Zero;
        }
        
        /// <summary>
        /// Get time until end (from given UTC time)
        /// </summary>
        public TimeSpan GetTimeUntilEnd(DateTime utcTime)
        {
            return EndDate > utcTime ? EndDate - utcTime : TimeSpan.Zero;
        }
        
        /// <summary>
        /// Check if promo code applies to specific category
        /// </summary>
        public bool AppliesTo(int? categoryId)
        {
            return !CategoryId.HasValue || CategoryId.Value == categoryId;
        }
        
        /// <summary>
        /// Check if promo code applies to customer tier
        /// </summary>
        public bool AppliesTo(string? customerTier)
        {
            return string.IsNullOrEmpty(CustomerTier) || 
                   CustomerTier == "All" || 
                   CustomerTier == customerTier;
        }
        
        /// <summary>
        /// Calculate discount for given amount
        /// </summary>
        public decimal CalculateDiscount(decimal amount)
        {
            var discount = Type switch
            {
                PromoCodeType.Percentage => (amount * DiscountValue) / 100,
                PromoCodeType.FixedAmount => Math.Min(DiscountValue, amount),
                PromoCodeType.FreeShipping => 0, // Handled separately
                _ => 0
            };
            
            // Apply maximum discount cap
            if (MaxDiscountAmount.HasValue)
            {
                discount = Math.Min(discount, MaxDiscountAmount.Value);
            }
            
            return discount;
        }
        
        /// <summary>
        /// Get formatted discount text
        /// </summary>
        public string GetFormattedDiscount()
        {
            return Type switch
            {
                PromoCodeType.Percentage => $"{DiscountValue}% OFF",
                PromoCodeType.FixedAmount => $"Rs.{DiscountValue} OFF",
                PromoCodeType.FreeShipping => "FREE SHIPPING",
                _ => "DISCOUNT APPLIED"
            };
        }

        /// <summary>
        /// Record usage of this promo code
        /// </summary>
        public PromoCodeUsage RecordUsage(int userId, decimal discountAmount, string usageContext = "Cart", int? orderId = null)
        {
            var usage = new PromoCodeUsage
            {
                PromoCodeId = Id,
                UserId = userId,
                DiscountAmount = discountAmount,
                OrderId = orderId,
                UsageContext = usageContext,
                UsedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            PromoCodeUsages.Add(usage);
            CurrentUsageCount++;
            UpdatedAt = DateTime.UtcNow;

            return usage;
        }
        /// <summary>
        ///  NEW: Create usage record without adding to collection (for explicit DB insert)
        /// </summary>
        public PromoCodeUsage CreateUsageRecord(int userId, decimal discountAmount, string usageContext = "Cart", int? orderId = null)
        {
            return new PromoCodeUsage
            {
                PromoCodeId = Id,
                UserId = userId,
                DiscountAmount = discountAmount,
                OrderId = orderId,
                UsageContext = usageContext,
                UsedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        /// <summary>
        ///  NEW: Increment usage count (for database updates)
        /// </summary>
        public void IncrementUsageCount()
        {
            CurrentUsageCount++;
            UpdatedAt = DateTime.UtcNow;
        }
        
        
        /// <summary>
        /// Validate minimum order amount requirement
        /// </summary>
        public bool ValidateMinOrderAmount(decimal orderAmount)
        {
            return !MinOrderAmount.HasValue || orderAmount >= MinOrderAmount.Value;
        }
    }
}