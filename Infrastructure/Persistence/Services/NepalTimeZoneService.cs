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
             try
            {
                //  TRY to use system timezone first (more accurate)
                return TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
            }
            catch
            {
                // FALLBACK: Create custom timezone
                return TimeZoneInfo.CreateCustomTimeZone(
                    id: "Nepal Standard Time",
                    baseUtcOffset: NepalOffset,
                    displayName: "(UTC+05:45) Nepal Standard Time",
                    standardDisplayName: "Nepal Standard Time"
                );
            }
        }

        public DateTime GetNepalCurrentTime()
        {
           var cacheKey = $"{CACHE_KEY_PREFIX}current_nepal";

            //  Use simple time-based caching
            if (_cache.TryGetValue(cacheKey, out DateTime cachedTime))
            {
                // Cache is valid for 1 second
                return cachedTime;
            }

            var utcNow = DateTime.UtcNow;
            var nepalTime = ConvertFromUtcToNepal(utcNow);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1),
                Size = 1,
                Priority = CacheItemPriority.High
            };

            // Cache for 1 second with absolute expiration
            _cache.Set(cacheKey, nepalTime, cacheOptions);

            _logger.LogTrace(" Current time - UTC: {UtcTime} → Nepal: {NepalTime}",
                utcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                nepalTime.ToString("yyyy-MM-dd HH:mm:ss"));

            return nepalTime;
        }

        public DateTime ConvertFromUtcToNepal(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Local)
            {
                _logger.LogWarning(" Received Local DateTime, converting to UTC first: {DateTime}", utcDateTime);
                utcDateTime = utcDateTime.ToUniversalTime();
            }
            else if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                _logger.LogWarning(" Received Unspecified DateTime, treating as UTC: {DateTime}", utcDateTime);
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            try
            {
                var nepalTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _nepalTimeZone);
                
                _logger.LogTrace("Converted UTC {UtcTime} → Nepal {NepalTime}",
                    utcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    nepalTime.ToString("yyyy-MM-dd HH:mm:ss"));

                return nepalTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting UTC to Nepal time: {DateTime}", utcDateTime);
                // FALLBACK: Manual conversion
                return utcDateTime.Add(NepalOffset);
            }
        }

        public DateTime ConvertFromNepalToUtc(DateTime nepalDateTime)
        {
            // VALIDATE: Ensure we're treating this as Nepal time
            if (nepalDateTime.Kind == DateTimeKind.Utc)
            {
                _logger.LogWarning(" Received UTC DateTime in ConvertFromNepalToUtc: {DateTime}", nepalDateTime);
                return nepalDateTime; // Already UTC
            }

            try
            {
                // CORRECT: Specify that the input is in Nepal timezone
                var nepalAsUnspecified = DateTime.SpecifyKind(nepalDateTime, DateTimeKind.Unspecified);
                var utcTime = TimeZoneInfo.ConvertTimeToUtc(nepalAsUnspecified, _nepalTimeZone);
                
                _logger.LogTrace("Converted Nepal {NepalTime} → UTC {UtcTime}",
                    nepalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    utcTime.ToString("yyyy-MM-dd HH:mm:ss"));

                return utcTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error converting Nepal to UTC time: {DateTime}", nepalDateTime);
                // FALLBACK: Manual conversion
                return nepalDateTime.Subtract(NepalOffset);
            }
        }

        public DateTime GetUtcCurrentTime() => DateTime.UtcNow;

        public bool IsTimeBetween(DateTime startTime, DateTime endTime, DateTime? checkTime = null)
        {
            var timeToCheck = checkTime ?? GetUtcCurrentTime();

            // ENSURE ALL COMPARISONS ARE IN UTC
            var utcStart = ToUtcSafely(startTime);
            var utcEnd = ToUtcSafely(endTime);
            var utcCheck = ToUtcSafely(timeToCheck);

            var result = utcCheck >= utcStart && utcCheck <= utcEnd;

            _logger.LogDebug(" Time check: {CheckTime} between {StartTime} and {EndTime} = {Result}",
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
            var utcStart = ToUtcSafely(eventStartUtc);
            return utcStart > now ? utcStart - now : TimeSpan.Zero;
        }

        public TimeSpan GetTimeUntilEventEnd(DateTime eventEndUtc)
        {
            var now = GetUtcCurrentTime();
            var utcEnd = ToUtcSafely(eventEndUtc);
            return utcEnd > now ? utcEnd - now : TimeSpan.Zero;
        }

        public string GetEventTimeStatus(DateTime startTime, DateTime endTime)
        {
            var now = GetUtcCurrentTime();
            var utcStart = ToUtcSafely(startTime);
            var utcEnd = ToUtcSafely(endTime);

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
            var utcTime = ToUtcSafely(dateTime);
            return utcTime.ToString(format) + " UTC";
        }

        public DateTime ToUtcSafely(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(), //  CORRECT: Use system local timezone
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), // ASSUME UTC for unspecified
                _ => dateTime
            };
        }

        public DateTime EnsureUtc(DateTime dateTime) => ToUtcSafely(dateTime);       

        public bool IsUtcTime(DateTime dateTime) => dateTime.Kind == DateTimeKind.Utc;
        

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

            _logger.LogDebug(" Batch processed {Count} events for active status", results.Count);
            return results;
        }

         public bool ValidateTimeRange(DateTime start, DateTime end)
        {
            var utcStart = ToUtcSafely(start);
            var utcEnd = ToUtcSafely(end);
            
            if (utcEnd <= utcStart)
            {
                _logger.LogWarning("Invalid time range: End time {End} is not after start time {Start}", 
                    utcEnd, utcStart);
                return false;
            }

            return true;
        }

        public (DateTime utcStart, DateTime utcEnd) NormalizeTimeRange(DateTime start, DateTime end)
        {
            var utcStart = ToUtcSafely(start);
            var utcEnd = ToUtcSafely(end);

            if (utcEnd < utcStart)
            {
                _logger.LogWarning("⚠️ Swapping invalid time range: {Start} - {End}", utcStart, utcEnd);
                (utcStart, utcEnd) = (utcEnd, utcStart);
            }

            return (utcStart, utcEnd);
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            return $"{timeSpan.Seconds}s";
        }
    }
}
