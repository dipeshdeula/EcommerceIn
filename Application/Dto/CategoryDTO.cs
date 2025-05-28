using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Application.Dto
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }  // e.g. "home-appliances"
        public string Description { get; set; } 
        public string ImageUrl { get; set; } = string.Empty;
        public List<SubCategoryDTO> SubCategories { get; set; } = new List<SubCategoryDTO>();

    }
}

