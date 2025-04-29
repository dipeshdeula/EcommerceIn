using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllProductsByCategoryId(
         int CategoryId,
        int PageNumber,
        int PageSize
        ) : IRequest<Result<CategoryWithProductsDTO>>;

    public class GetAllProductsByCategoryIdHandler : IRequestHandler<GetAllProductsByCategoryId, Result<CategoryWithProductsDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        public GetAllProductsByCategoryIdHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
            
        }
        public async Task<Result<CategoryWithProductsDTO>> Handle(GetAllProductsByCategoryId request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<CategoryWithProductsDTO>.Failure("CategoryId is not found");
            }

            // Fetch products by CategoryId with pagination
            var products = await _categoryRepository.GetProductsByCategoryIdAsync(
                request.CategoryId,
                (request.PageNumber - 1) * request.PageSize,
                request.PageSize
                );

            // Map products to ProductDTO
            var productDtos = products.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Images = p.Images.Select(img=> new ProductImageDTO { 
                    Id = img.Id,
                    ImageUrl = img.ImageUrl
                }).ToList()
            }).ToList();

            // Map category to CategoryWithProductsDTO
            var categoryWithProductsDto = new CategoryWithProductsDTO
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Products = productDtos // Add products to the DTO
            };

            return Result<CategoryWithProductsDTO>.Success(categoryWithProductsDto, "Products fetched successfully");
        }
    }

}
