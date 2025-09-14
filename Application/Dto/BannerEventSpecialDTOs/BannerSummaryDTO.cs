using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class BannerSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TagLine { get; set; }
        public decimal DiscountValue { get; set; }
        public PromotionType PromotionType { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDateNepal { get; set; }
        public DateTime EndDateNepal { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public int DaysRemaining { get; set; }    

        public ICollection<BannerImageDTO> Images { get; set; } = new List<BannerImageDTO>();
    }
}
