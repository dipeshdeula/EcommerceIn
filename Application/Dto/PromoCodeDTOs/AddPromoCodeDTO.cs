using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.PromoCodeDTOs
{
    public class AddPromoCodeDTO
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public PromoCodeType Type { get; set; } = PromoCodeType.Percentage;

        [Range(0, 100)]
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; } = false;

        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }

        public int? MaxTotalUsage { get; set; }
        public int? MaxUsagePerUser { get; set; }

        //  NEPAL TIME INPUTS - Using STRING format to avoid DateTime.Kind issues
        [JsonPropertyName("startDateNepal")]
        [Required]
        public string StartDateNepal { get; set; } = string.Empty;

        [JsonPropertyName("endDateNepal")]
        [Required]
        public string EndDateNepal { get; set; } = string.Empty;

        public bool ApplyToShipping { get; set; } = false;
        public bool StackableWithEvents { get; set; } = true;

        [StringLength(50)]
        public string? CustomerTier { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        //  HELPER PROPERTIES
        [JsonIgnore]
        public DateTime StartDateParsed
        {
            get
            {
                if (DateTime.TryParse(StartDateNepal, out var date))
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                return DateTime.MinValue;
            }
        }

        [JsonIgnore]
        public DateTime EndDateParsed
        {
            get
            {
                if (DateTime.TryParse(EndDateNepal, out var date))
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                return DateTime.MinValue;
            }
        }

        [JsonIgnore]
        public bool IsValidDateRange => EndDateParsed > StartDateParsed && StartDateParsed != DateTime.MinValue;
    }
}
