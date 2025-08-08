using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Extension
{
    /// <summary>
    /// ✅ Application layer extensions for PromoCode with timezone support
    /// </summary>
    public static class PromoCodeExtensions
    {
        /// <summary>
        /// Check if promo code is valid right now in Nepal timezone
        /// </summary>
        public static bool IsValidNow(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            var currentUtc = timeZoneService.GetUtcCurrentTime();
            return promoCode.IsValidAtTime(currentUtc);
        }

        /// <summary>
        /// Get status in Nepal timezone context
        /// </summary>
        public static string GetStatusInNepalTime(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.GetEventTimeStatus(promoCode.StartDate, promoCode.EndDate);
        }

        /// <summary>
        /// Check if expired using Nepal timezone service
        /// </summary>
        public static bool IsExpiredNow(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            var currentUtc = timeZoneService.GetUtcCurrentTime();
            return promoCode.IsExpiredAtTime(currentUtc);
        }

        /// <summary>
        /// Check if promo code is currently active (considering Nepal timezone)
        /// </summary>
        public static bool IsCurrentlyActive(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            return promoCode.IsActive && 
                   timeZoneService.IsEventActiveNow(promoCode.StartDate, promoCode.EndDate);
        }

        /// <summary>
        /// Get time remaining until start (in Nepal timezone context)
        /// </summary>
        public static TimeSpan? GetTimeUntilStart(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            var currentUtc = timeZoneService.GetUtcCurrentTime();
            var timeUntil = promoCode.GetTimeUntilStart(currentUtc);
            return timeUntil > TimeSpan.Zero ? timeUntil : null;
        }

        /// <summary>
        /// Get time remaining until end (in Nepal timezone context)
        /// </summary>
        public static TimeSpan? GetTimeUntilEnd(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            var currentUtc = timeZoneService.GetUtcCurrentTime();
            var timeUntil = promoCode.GetTimeUntilEnd(currentUtc);
            return timeUntil > TimeSpan.Zero ? timeUntil : null;
        }

        /// <summary>
        /// Get days remaining (Nepal timezone context)
        /// </summary>
        public static int GetDaysRemaining(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            var timeUntilEnd = promoCode.GetTimeUntilEnd(timeZoneService);
            return timeUntilEnd.HasValue ? (int)Math.Ceiling(timeUntilEnd.Value.TotalDays) : 0;
        }

        /// <summary>
        /// Check if promo code is expiring soon (within 24 hours)
        /// </summary>
        public static bool IsExpiringSoon(this PromoCode promoCode, INepalTimeZoneService timeZoneService, int hoursThreshold = 24)
        {
            var timeUntilEnd = promoCode.GetTimeUntilEnd(timeZoneService);
            return timeUntilEnd.HasValue && timeUntilEnd.Value.TotalHours <= hoursThreshold;
        }

        /// <summary>
        /// Get user-friendly status message
        /// </summary>
        public static string GetUserFriendlyStatus(this PromoCode promoCode, INepalTimeZoneService timeZoneService)
        {
            var currentUtc = timeZoneService.GetUtcCurrentTime();
            
            if (!promoCode.IsActive)
                return "Inactive";
            
            if (promoCode.IsDeleted)
                return "Deleted";
            
            if (!promoCode.IsStartedAtTime(currentUtc))
            {
                var startNepal = timeZoneService.ConvertFromUtcToNepal(promoCode.StartDate);
                return $"Starts {startNepal:MMM dd, yyyy HH:mm} NPT";
            }
            
            if (promoCode.IsExpiredAtTime(currentUtc))
            {
                var endNepal = timeZoneService.ConvertFromUtcToNepal(promoCode.EndDate);
                return $"Expired {endNepal:MMM dd, yyyy HH:mm} NPT";
            }
            
            if (!promoCode.HasUsageRemaining())
                return "Usage limit reached";
            
            var daysRemaining = promoCode.GetDaysRemaining(timeZoneService);
            if (daysRemaining <= 1)
                return "Expires today";
            
            if (daysRemaining <= 7)
                return $"Expires in {daysRemaining} days";
            
            return "Active";
        }

        /// <summary>
        /// Get validation errors for promo code usage
        /// </summary>
        public static List<string> GetValidationErrors(this PromoCode promoCode, INepalTimeZoneService timeZoneService, 
            int userId, decimal orderAmount, int? categoryId = null, string? customerTier = null)
        {
            var errors = new List<string>();
            var currentUtc = timeZoneService.GetUtcCurrentTime();
            
            if (!promoCode.IsActive)
                errors.Add("This promo code is currently inactive");
            
            if (promoCode.IsDeleted)
                errors.Add("This promo code is no longer available");
            
            if (!promoCode.IsStartedAtTime(currentUtc))
            {
                var startNepal = timeZoneService.ConvertFromUtcToNepal(promoCode.StartDate);
                errors.Add($"This promo code is not valid until {startNepal:MMM dd, yyyy HH:mm} NPT");
            }
            
            if (promoCode.IsExpiredAtTime(currentUtc))
            {
                var endNepal = timeZoneService.ConvertFromUtcToNepal(promoCode.EndDate);
                errors.Add($"This promo code expired on {endNepal:MMM dd, yyyy HH:mm} NPT");
            }
            
            if (!promoCode.HasUsageRemaining())
                errors.Add("This promo code has reached its usage limit");
            
            if (!promoCode.CanUserUse(userId))
                errors.Add($"You have already used this promo code the maximum number of times ({promoCode.MaxUsagePerUser})");
            
            if (!promoCode.ValidateMinOrderAmount(orderAmount))
                errors.Add($"Minimum order amount of Rs.{promoCode.MinOrderAmount:F2} required (current: Rs.{orderAmount:F2})");
            
            if (!promoCode.AppliesTo(categoryId))
                errors.Add("This promo code is not valid for items in your cart");
            
            if (!promoCode.AppliesTo(customerTier))
                errors.Add($"This promo code is only valid for {promoCode.CustomerTier} customers");
            
            return errors;
        }
    }
}