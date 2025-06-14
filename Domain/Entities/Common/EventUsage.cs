using System;
using System.Collections.Generic;
using System.Linq;
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
        public int? OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
    }
}
