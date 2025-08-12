using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Common.Helper;
using Application.Common;
using Domain.Enums;

namespace Application.Dto.PromoCodeDTOs
{
    public class AddPromoCodeDTO
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public PromoCodeType Type { get; set; }

        public string? Description { get; set; }

        [Required]
        [Range(0.01, 100)]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxDiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxTotalUsage { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxUsagePerUser { get; set; }

        // ✅ USE CONSISTENT DATE INPUT FORMAT WITH TimeParsingHelper
        [Required]
        [JsonPropertyName("startDateNepal")]
        public string StartDateNepal { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("endDateNepal")]
        public string EndDateNepal { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool ApplyToShipping { get; set; } = false;
        public bool StackableWithEvents { get; set; } = false;

        public string? CustomerTier { get; set; }

        // ✅ PARSING HELPERS USING TimeParsingHelper
        [JsonIgnore]
        public DateTime? StartDateParsed
        {
            get
            {
                var result = TimeParsingHelper.ParseFlexibleDateTime(StartDateNepal);
                return result.Succeeded ? result.Data : null;
            }
        }

        [JsonIgnore]
        public DateTime? EndDateParsed
        {
            get
            {
                var result = TimeParsingHelper.ParseFlexibleDateTime(EndDateNepal);
                return result.Succeeded ? result.Data : null;
            }
        }

        [JsonIgnore]
        public bool HasValidDateFormats => StartDateParsed.HasValue && EndDateParsed.HasValue;

        [JsonIgnore]
        public bool IsValidDateRange => HasValidDateFormats && EndDateParsed > StartDateParsed;

        [JsonIgnore]
        public string? StartDateParsingError
        {
            get
            {
                var result = TimeParsingHelper.ParseFlexibleDateTime(StartDateNepal);
                return result.Succeeded ? null : result.Message;
            }
        }

        [JsonIgnore]
        public string? EndDateParsingError
        {
            get
            {
                var result = TimeParsingHelper.ParseFlexibleDateTime(EndDateNepal);
                return result.Succeeded ? null : result.Message;
            }
        }
    }
}