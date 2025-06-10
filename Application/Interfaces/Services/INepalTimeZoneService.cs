using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface INepalTimeZoneService
    {
        /// <summary>
        /// Gets current time in Nepal Time Zone (NPT)
        /// </summary>
        /// 
        //  CORE TIME OPERATIONS
        DateTime GetNepalCurrentTime();
        DateTime ConvertFromUtcToNepal(DateTime utcDateTime);
        DateTime ConvertFromNepalToUtc(DateTime nepalDateTime);
        DateTime GetUtcCurrentTime();

        //  BUSINESS LOGIC HELPERS
        bool IsTimeBetween(DateTime startTime, DateTime endTime, DateTime? checkTime = null);
        bool IsEventActiveNow(DateTime eventStartUtc, DateTime eventEndUtc);
        TimeSpan GetTimeUntilEventStart(DateTime eventStartUtc);
        TimeSpan GetTimeUntilEventEnd(DateTime eventEndUtc);

        //  FORMATTING & DISPLAY
        string FormatNepalTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss");
        string FormatUtcTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss");
        string GetEventTimeStatus(DateTime startTime, DateTime endTime);

        // VALIDATION & CONVERSION
        DateTime EnsureUtc(DateTime dateTime);
        bool IsUtcTime(DateTime dateTime);
        TimeZoneInfo GetNepalTimeZone();

        // BATCH OPERATIONS (for performance)
        Dictionary<int, bool> CheckMultipleEventsActive(IEnumerable<(int Id, DateTime Start, DateTime End)> events);
    }
}
