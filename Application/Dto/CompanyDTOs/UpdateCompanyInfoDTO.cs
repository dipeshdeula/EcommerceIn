using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CompanyDTOs
{
    public class UpdateCompanyInfoDTO
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? RegisteredPanNumber { get; set; }       
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? WebsiteUrl { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
