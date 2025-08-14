using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CategoryDTOs
{
    public class CategoryWithSubCategoryDTO
    {
        public int Id { get; set; } // CategoryId
        public string Name { get; set; } = string.Empty; // CategoryName
        public string Slug { get; set; } = string.Empty; // category slug
        public string Description { get; set; } = string.Empty; // category Description

        public IEnumerable<SubCategoryDTO> SubCategories { get; set; } = new List<SubCategoryDTO>();

        public int TotalSubCategories { get; set; }
    }
}
