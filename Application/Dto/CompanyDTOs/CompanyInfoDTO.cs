using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CompanyDTOs
{
    public class CompanyInfoDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string RegisteredPanNumber { get; set; } = string.Empty;        
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
