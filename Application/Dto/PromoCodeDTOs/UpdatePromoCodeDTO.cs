using Application.Common.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.PromoCodeDTOs
{
     public class UpdatePromoCodeDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        public decimal? DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }

        public int? MaxTotalUsage { get; set; }
        public int? MaxUsagePerUser { get; set; }

        // ✅ NEPAL TIMEZONE DATE INPUTS (using TimeParsingHelper)
        [JsonPropertyName("startDateNepal")]
        public string? StartDateNepal { get; set; }

        [JsonPropertyName("endDateNepal")]
        public string? EndDateNepal { get; set; }

        public bool? IsActive { get; set; }
        public bool? ApplyToShipping { get; set; }
        public bool? StackableWithEvents { get; set; }

        public string? AdminNotes { get; set; }

        // ✅ HELPER PROPERTIES USING TimeParsingHelper
        [JsonIgnore]
        public DateTime? StartDateParsed
        {
            get
            {
                if (string.IsNullOrEmpty(StartDateNepal)) return null;
                var result = TimeParsingHelper.ParseFlexibleDateTime(StartDateNepal);
                return result.Succeeded ? result.Data : null;
            }
        }

        [JsonIgnore]
        public DateTime? EndDateParsed
        {
            get
            {
                if (string.IsNullOrEmpty(EndDateNepal)) return null;
                var result = TimeParsingHelper.ParseFlexibleDateTime(EndDateNepal);
                return result.Succeeded ? result.Data : null;
            }
        }

        [JsonIgnore]
        public bool HasDateUpdates => !string.IsNullOrEmpty(StartDateNepal) || !string.IsNullOrEmpty(EndDateNepal);

        [JsonIgnore]
        public bool IsValidDateRange
        {
            get
            {
                if (!HasDateUpdates) return true;
                
                var start = StartDateParsed;
                var end = EndDateParsed;
                
                if (start.HasValue && end.HasValue)
                    return end.Value > start.Value;
                
                return true; // Partial updates are allowed
            }
        }

        // ✅ GET PARSING ERROR MESSAGES
        [JsonIgnore]
        public string? StartDateParsingError
        {
            get
            {
                if (string.IsNullOrEmpty(StartDateNepal)) return null;
                var result = TimeParsingHelper.ParseFlexibleDateTime(StartDateNepal);
                return result.Succeeded ? null : result.Message;
            }
        }

        [JsonIgnore]
        public string? EndDateParsingError
        {
            get
            {
                if (string.IsNullOrEmpty(EndDateNepal)) return null;
                var result = TimeParsingHelper.ParseFlexibleDateTime(EndDateNepal);
                return result.Succeeded ? null : result.Message;
            }
        }

        // ✅ VALIDATION HELPER
        [JsonIgnore]
        public bool HasValidDateFormats => 
            (string.IsNullOrEmpty(StartDateNepal) || StartDateParsed.HasValue) &&
            (string.IsNullOrEmpty(EndDateNepal) || EndDateParsed.HasValue);
    }
}
