using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Application.Dto.StoreDTOs
{
    public class UpdateStoreDTO
    {
        [FromForm]
        public string? Name { get; set; }
        [FromForm]
        public string? OwnerName { get; set; }
        [FromForm]
        public IFormFile? FIle { get; set; }

        [JsonIgnore]
        public bool IsDeleted { get; set; }
    }
}
