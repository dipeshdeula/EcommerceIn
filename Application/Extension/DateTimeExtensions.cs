using Application.Interfaces.Services;

namespace Application.Extension
{
    public static class DateTimeExtensions
    {
        // BASIC CONVERSIONS
        public static DateTime ToNepalTime(this DateTime utcDateTime, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.ConvertFromUtcToNepal(utcDateTime);
        }

        public static DateTime ToUtcFromNepal(this DateTime nepalDateTime, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.ConvertFromNepalToUtc(nepalDateTime);
        }

        public static string ToNepalTimeString(this DateTime dateTime, INepalTimeZoneService timeZoneService, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return timeZoneService.FormatNepalTime(dateTime, format);
        }

        //  BUSINESS LOGIC EXTENSIONS
        public static bool IsBetweenNepalTime(this DateTime checkTime, DateTime startTime, DateTime endTime, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.IsTimeBetween(startTime, endTime, checkTime);
        }

        public static bool IsEventActiveNow(this DateTime eventStart, DateTime eventEnd, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.IsEventActiveNow(eventStart, eventEnd);
        }

        public static string GetEventStatus(this DateTime eventStart, DateTime eventEnd, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.GetEventTimeStatus(eventStart, eventEnd);
        }

        // SAFE UTC CONVERSION
        public static DateTime EnsureUtc(this DateTime dateTime, INepalTimeZoneService timeZoneService)
        {
            return timeZoneService.EnsureUtc(dateTime);
        }

        //  VALIDATION HELPERS
        public static bool IsValidEventDateRange(this DateTime startDate, DateTime endDate)
        {
            return endDate > startDate && startDate >= DateTime.UtcNow.AddMinutes(-5); // Allow 5 min buffer
        }

        //  FORMATTING HELPERS
        public static string ToUserFriendlyNepalTime(this DateTime dateTime, INepalTimeZoneService timeZoneService)
        {
            var nepalTime = timeZoneService.ConvertFromUtcToNepal(dateTime);
            var now = timeZoneService.GetNepalCurrentTime();

            var diff = nepalTime - now;

            if (Math.Abs(diff.TotalDays) < 1)
            {
                return nepalTime.ToString("HH:mm") + " (Today)";
            }
            else if (Math.Abs(diff.TotalDays) < 7)
            {
                return nepalTime.ToString("ddd, HH:mm");
            }
            else
            {
                return nepalTime.ToString("MMM dd, HH:mm");
            }
        }
    }
}
