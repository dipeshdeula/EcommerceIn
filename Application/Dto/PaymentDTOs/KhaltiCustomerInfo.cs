using System.Text.Json.Serialization;

namespace Application.Dto.PaymentDTOs
{
    public class KhaltiCustomerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;
    }
}
