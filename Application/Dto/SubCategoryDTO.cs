using Application.Dto.CategoryDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class SubCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // e.g. "home-appliances"
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public int CategoryId { get; set; } // Foreign key to Category
        
        public CategoryDTO? categoryDTO { get; set; }
        public List<SubSubCategoryDTO> SubSubCategories { get; set; } = new List<SubSubCategoryDTO>();
    }
}
