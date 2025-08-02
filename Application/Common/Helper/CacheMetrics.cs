using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Helper
{
    public class CacheMetrics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long Writes { get; set; }
        public long Errors { get; set; }
        public long Total { get; set; }
        public long TotalLatency { get; set; }
    }
}
