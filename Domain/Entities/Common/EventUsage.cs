using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class EventUsage 
    {
        public int Id { get; set; }
        public int BannerEventId { get; set; }
        public virtual BannerEventSpecial BannerEvent { get; set; } = null!;
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }
        public decimal? DiscountApplied { get; set; } = 0;
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public int ProductsCount { get; set; } = 1;
        public decimal OrderValue { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}
