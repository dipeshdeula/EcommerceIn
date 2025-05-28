using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class UpdateCategoryDTO
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
