using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Common.Helper
{
    public class TimeZoneDisplayInfo
    {
        [JsonPropertyName("displayTimZone")]
        public string DisplayTimeZone { get; set; } = "Nepal Standard Time";

        [JsonPropertyName("offsetString")]
        public string OffsetString { get; set; } = "UTC+05:45";

        [JsonPropertyName("currentNepalTime")]
        public string CurrentNepalTime { get; set; } = string.Empty;

        [JsonPropertyName("currentUtcTime")]
        public string CurrentUtcTime { get; set; } = string.Empty;

        [JsonPropertyName("timeZoneAbbreviation")]
        public string TimeZoneAbbreviation { get; set; } = "NPT";

        [JsonPropertyName("isDaylightSavingTime")]
        public bool IsDaylightSavingTime { get; set; } = false; // Nepal doesn't use DST

         [JsonPropertyName("offsetHours")]
        public double OffsetHours { get; set; } = 5.75; // 5 hours 45 minutes

        [JsonPropertyName("formattedOffset")]
        public string FormattedOffset => "+05:45";

        [JsonPropertyName("timeZoneName")]
        public string TimeZoneName { get; set; } = "Asia/Kathmandu";

        [JsonPropertyName("isNepalTime")]
        public bool IsNepalTime { get; set; } = true;
    }
}
