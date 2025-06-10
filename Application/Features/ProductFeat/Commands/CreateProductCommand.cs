using Application.Common;
using Application.Dto;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Features.SubSubCategoryFeat.Module;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.ProductFeat.Commands
{
    public record CreateProductCommand(  
        int SubSubCategoryId,
        CreateProductDTO createProductDTO

        ) : IRequest<Result<ProductDTO>>;

    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDTO>>
    {
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        public CreateProductCommandHandler(ISubSubCategoryRepository subSubCategoryRepository)
        {
            _subSubCategoryRepository = subSubCategoryRepository;

        }
        public async Task<Result<ProductDTO>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Validate SubSubCategoryId
            var subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.SubSubCategoryId);
            if (subSubCategory == null)
            {
                return Result<ProductDTO>.Failure("sub-sub category not found");
            }

            // Resolve CategoryId from SubSubCategory -> SubCategory -> Category
            var categoryId = subSubCategory.SubCategory?.CategoryId;
            if (categoryId == null)
            {
                return Result<ProductDTO>.Failure("Category not found for the given sub-sub category");
            }

            // Create the new Product item
            var product = new Product
            {
                SubSubCategoryId = subSubCategory.Id,
                CategoryId = categoryId.Value,
                Name = request.createProductDTO.Name,
                Slug = request.createProductDTO.Slug,
                Description = request.createProductDTO.Slug,
                MarketPrice = request.createProductDTO.MarketPrice,
                CostPrice = request.createProductDTO.CostPrice,
                DiscountPrice = request.createProductDTO.DiscountPrice,
                StockQuantity = request.createProductDTO.StockQuantity,
                Sku = request.createProductDTO.Sku,
                Weight = request.createProductDTO.Weight,
                Reviews = request.createProductDTO.Reviews,
                Rating = request.createProductDTO.Rating,
                Dimensions = request.createProductDTO.Dimensions,
                SubSubCategory = subSubCategory

            };

            // Add the SubSubCategory to the parent's subsubcategories collection
            subSubCategory.Products.Add(product);

            // save changes to the database
            await _subSubCategoryRepository.UpdateAsync(subSubCategory, cancellationToken);
            await _subSubCategoryRepository.SaveChangesAsync(cancellationToken);

            // Map to the Dto and return success
            return Result<ProductDTO>.Success(product.ToDTO(), "Product item created successfully");
        }
    }

}
