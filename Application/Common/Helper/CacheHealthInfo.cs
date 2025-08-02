using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Helper
{
    public class CacheHealthInfo
    {
        public bool IsRedisConnected { get; set; }
        public bool IsMemoryCacheHealthy { get; set; }
        public TimeSpan RedisLatency { get; set; }
        public long RedisMemoryUsage { get; set; }
        public Dictionary<string, int> CacheHitRates { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
