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
        public int ProductId { get; set; }
        public decimal? SpecificDiscount { get; set; } // Override event discount
        public DateTime AddedAt { get; set; }
        public BannerEventSpecial BannerEvent { get; set; }
        public Product Product { get; set; } 

    }
}
