using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class NepalTimeZoneService : INepalTimeZoneService
    {
        private readonly ILogger<NepalTimeZoneService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeZoneInfo _nepalTimeZone;
        private static readonly TimeSpan NepalOffset = TimeSpan.FromMinutes(345); // UTC+5:45
        private const string CACHE_KEY_PREFIX = "nepal_time_";

        public NepalTimeZoneService(ILogger<NepalTimeZoneService> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
            _nepalTimeZone = CreateNepalTimeZone();

            _logger.LogInformation("🇳🇵 Nepal Time Zone Service initialized. Current Nepal Time: {NepalTime}",
                GetNepalCurrentTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private static TimeZoneInfo CreateNepalTimeZone()
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                id: "Nepal Standard Time",
                baseUtcOffset: NepalOffset,
                displayName: "(UTC+05:45) Nepal Standard Time",
                standardDisplayName: "Nepal Standard Time"
            );
        }

        public DateTime GetNepalCurrentTime()
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}current_nepal";

            if (_cache.TryGetValue(cacheKey, out DateTime cachedTime))
            {
                // Check if cached time is within 1 second (for performance)
                if (DateTime.UtcNow.Subtract(cachedTime.Subtract(NepalOffset)).TotalSeconds < 1)
                {
                    return cachedTime;
                }
            }

            var utcNow = DateTime.UtcNow;
            var nepalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _nepalTimeZone);

            // Cache for 1 second to improve performance
            _cache.Set(cacheKey, nepalTime, TimeSpan.FromSeconds(1));

            _logger.LogDebug("🕐 UTC: {UtcTime} → Nepal: {NepalTime}",
                utcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                nepalTime.ToString("yyyy-MM-dd HH:mm:ss"));

            return nepalTime;
        }

        public DateTime ConvertFromUtcToNepal(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                _logger.LogWarning("⚠️ Converting non-UTC DateTime to Nepal time: {DateTime}", utcDateTime);
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _nepalTimeZone);
        }

        public DateTime ConvertFromNepalToUtc(DateTime nepalDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(nepalDateTime, _nepalTimeZone);
        }

        public DateTime GetUtcCurrentTime() => DateTime.UtcNow;

        public bool IsTimeBetween(DateTime startTime, DateTime endTime, DateTime? checkTime = null)
        {
            var timeToCheck = checkTime ?? GetUtcCurrentTime();

            // ENSURE ALL COMPARISONS ARE IN UTC
            var utcStart = EnsureUtc(startTime);
            var utcEnd = EnsureUtc(endTime);
            var utcCheck = EnsureUtc(timeToCheck);

            var result = utcCheck >= utcStart && utcCheck <= utcEnd;

            _logger.LogDebug("⏰ Time check: {CheckTime} between {StartTime} and {EndTime} = {Result}",
                utcCheck.ToString("yyyy-MM-dd HH:mm:ss"),
                utcStart.ToString("yyyy-MM-dd HH:mm:ss"),
                utcEnd.ToString("yyyy-MM-dd HH:mm:ss"),
                result);

            return result;
        }

        public bool IsEventActiveNow(DateTime eventStartUtc, DateTime eventEndUtc)
        {
            var now = GetUtcCurrentTime();
            return IsTimeBetween(eventStartUtc, eventEndUtc, now);
        }

        public TimeSpan GetTimeUntilEventStart(DateTime eventStartUtc)
        {
            var now = GetUtcCurrentTime();
            var utcStart = EnsureUtc(eventStartUtc);
            return utcStart > now ? utcStart - now : TimeSpan.Zero;
        }

        public TimeSpan GetTimeUntilEventEnd(DateTime eventEndUtc)
        {
            var now = GetUtcCurrentTime();
            var utcEnd = EnsureUtc(eventEndUtc);
            return utcEnd > now ? utcEnd - now : TimeSpan.Zero;
        }

        public string GetEventTimeStatus(DateTime startTime, DateTime endTime)
        {
            var now = GetUtcCurrentTime();
            var utcStart = EnsureUtc(startTime);
            var utcEnd = EnsureUtc(endTime);

            if (now < utcStart)
            {
                var timeUntilStart = GetTimeUntilEventStart(utcStart);
                return $"Starts in {FormatTimeSpan(timeUntilStart)}";
            }
            else if (now >= utcStart && now <= utcEnd)
            {
                var timeUntilEnd = GetTimeUntilEventEnd(utcEnd);
                return $"Active - Ends in {FormatTimeSpan(timeUntilEnd)}";
            }
            else
            {
                return "Expired";
            }
        }

        public string FormatNepalTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var nepalTime = dateTime.Kind == DateTimeKind.Utc
                ? ConvertFromUtcToNepal(dateTime)
                : dateTime;
            return nepalTime.ToString(format);
        }

        public string FormatUtcTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var utcTime = EnsureUtc(dateTime);
            return utcTime.ToString(format) + " UTC";
        }

        public DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => TimeZoneInfo.ConvertTimeToUtc(dateTime, _nepalTimeZone),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _ => dateTime
            };
        }

        public bool IsUtcTime(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Utc;
        }

        public TimeZoneInfo GetNepalTimeZone() => _nepalTimeZone;

        //  BATCH OPERATIONS FOR PERFORMANCE
        public Dictionary<int, bool> CheckMultipleEventsActive(IEnumerable<(int Id, DateTime Start, DateTime End)> events)
        {
            var now = GetUtcCurrentTime();
            var results = new Dictionary<int, bool>();

            foreach (var (id, start, end) in events)
            {
                results[id] = IsTimeBetween(start, end, now);
            }

            _logger.LogDebug("🔄 Batch processed {Count} events for active status", results.Count);
            return results;
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
    }
}
