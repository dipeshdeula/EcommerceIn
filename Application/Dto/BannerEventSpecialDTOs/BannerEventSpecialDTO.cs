using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class BannerEventSpecialDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TagLine { get; set; }
        public EventType EventType { get; set; }
        public PromotionType PromotionType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? ActiveTimeSlot { get; set; }
        public int MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public int MaxUsagePerUser { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public EventStatus Status { get; set; }
        public ICollection<BannerImageDTO> Images { get; set; } = new List<BannerImageDTO>();
        public ICollection<EventRuleDTO> Rules { get; set; } = new List<EventRuleDTO>();
        public ICollection<EventProductDTO> EventProducts { get; set; } = new List<EventProductDTO>();
    }
}
