using Application.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extension
{
    public static class MappingExtensions
    {
        public static UserDTO ToDTO(this User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Contact = user.Contact,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl,
                IsDeleted = user.IsDeleted,
                Addresses = user.Addresses.Select(a => a.ToDTO()).ToList()
            };
        }

        public static AddressDTO ToDTO(this Address address)
        {
            return new AddressDTO
            {
                Id = address.Id,
                Label = address.Label,
                Street = address.Street,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault
            };
        }

        public static CategoryDTO ToDTO(this Category category)
        {
            return new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                SubCategories = category.SubCategories.Select(sc => sc.ToDTO()).ToList() // Map subcategories

            };
        }

        // Map SubCategory to SubCategoryDTO
        public static SubCategoryDTO ToDTO(this SubCategory subCategory)
        {
            return new SubCategoryDTO
            {
                Id = subCategory.Id,
                Name = subCategory.Name,
                Slug = subCategory.Slug,
                Description = subCategory.Description,
                ImageUrl = subCategory.ImageUrl,
                SubSubCategories = subCategory.SubSubCategories.Select(ssc => ssc.ToDTO()).ToList() // Map sub-subcategories
            };
        }

        // Map SubSubCategory to SubSubCategoryDTO
        public static SubSubCategoryDTO ToDTO(this SubSubCategory subSubCategory)
        {
            return new SubSubCategoryDTO
            {
                Id = subSubCategory.Id,
                Name = subSubCategory.Name,
                Slug = subSubCategory.Slug,
                Description = subSubCategory.Description,
                ImageUrl = subSubCategory.ImageUrl,
                SubCategoryId = subSubCategory.SubCategoryId,
                Products = subSubCategory.Products.Select(p => p.ToDTO()).ToList() // Map products
            };
        }

        // Map Product to ProductDTO
        public static ProductDTO ToDTO(this Product product)
        {
            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                IsDeleted = product.IsDeleted,
                Images = product.Images.Select(pi => pi.ToDTO()).ToList() // Map product images
            };
        }

        // Map ProductImage to ProductImageDTO
        public static ProductImageDTO ToDTO(this ProductImage productImage)
        {
            return new ProductImageDTO
            {
                Id = productImage.Id,
                ImageUrl = productImage.ImageUrl
            };
        }

    }
}
