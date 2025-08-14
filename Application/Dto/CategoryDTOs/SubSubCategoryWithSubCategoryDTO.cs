using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CategoryDTOs
{
    public class SubSubCategoryWithSubCategoryDTO
    {
        public int Id { get; set; } // SubCategoryId
        public string Name { get; set; } = string.Empty; // SubCategoryName
        public string Slug { get; set; } = string.Empty; // category slug
        public string Description { get; set; } = string.Empty; // category Description

        public IEnumerable<SubSubCategoryDTO> SubSubCategories { get; set; } = new List<SubSubCategoryDTO>();

        public int TotalSubSubCategories { get; set; }
    }
}
