using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class ProductImageDTO
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int ProductId { get; set; }  // foreign key

        // Properties or UI
        public string AltText { get; set; } = string.Empty;
        public bool IsMain { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
