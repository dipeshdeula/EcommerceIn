using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class EventProduct : BaseEntity
    {
        public int Id { get; set; }
        public int BannerEventId { get; set; }
        public virtual BannerEventSpecial BannerEvent { get; set; } = null!;
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        public decimal? SpecificDiscount { get; set; } // Override event discount
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
