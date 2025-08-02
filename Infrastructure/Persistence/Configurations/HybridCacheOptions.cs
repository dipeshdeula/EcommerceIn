using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class HybridCacheOptions
    {
        public const string SectionName = "HybridCache";

        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan L1CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
        public long MaxMemoryCacheSize { get; set; } = 1024; // 1024 units
        public bool EnableCompression { get; set; } = false; // Disable for now
        public bool EnableMetrics { get; set; } = true;

        public JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public Dictionary<string, TimeSpan> CachePolicies { get; set; } = new()
        {
            ["products"] = TimeSpan.FromMinutes(15),
            ["pricing"] = TimeSpan.FromMinutes(5),
            ["events"] = TimeSpan.FromMinutes(2),
            ["categories"] = TimeSpan.FromHours(1),
            ["search"] = TimeSpan.FromMinutes(10),
            ["cart"] = TimeSpan.FromMinutes(30),
            ["user"] = TimeSpan.FromMinutes(20)
        };

        //  Helper method to get cache expiry by type
        public TimeSpan GetExpiry(string cacheType)
        {
            return CachePolicies.TryGetValue(cacheType.ToLower(), out var expiry)
                ? expiry
                : DefaultExpiry;
        }
    }
}
