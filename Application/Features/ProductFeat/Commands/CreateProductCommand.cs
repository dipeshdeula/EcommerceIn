using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.ProductFeat.Commands
{
    public record CreateProductCommand(  
        int CategoryId,
        int? SubCategoryId,
        int? SubSubCategoryId,
        CreateProductDTO createProductDTO

        ) : IRequest<Result<ProductDTO>>;

    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly IProductRepository _productRepository;
        public CreateProductCommandHandler(
            ICategoryRepository categoryRepository,
         ISubCategoryRepository subCategoryRepository,
         ISubSubCategoryRepository subSubCategoryRepository,
         IProductRepository productRepository)
        {
            _categoryRepository = categoryRepository;
            _subCategoryRepository = subCategoryRepository;
            _subSubCategoryRepository = subSubCategoryRepository;
            _productRepository = productRepository;

        }
        public async Task<Result<ProductDTO>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // validate CategoryId
            var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
            if (category == null)
                return Result<ProductDTO>.Failure("Category not found");

            SubCategory? subCategory = null;
            SubSubCategory? subSubCategory = null;

            if (request.SubCategoryId.HasValue)
            {
                subCategory = await _subCategoryRepository.FindByIdAsync(request.SubCategoryId.Value);
                if (subCategory == null)
                    return Result<ProductDTO>.Failure("SubCategory not found");
            }

            if (request.SubSubCategoryId.HasValue)
            {
                subSubCategory = await _subSubCategoryRepository.FindByIdAsync(request.SubSubCategoryId.Value);
                if (subSubCategory == null)
                    return Result<ProductDTO>.Failure("SubSubCategory not found");
            }


            // Calculate discount price from percentage
            decimal? discountPrice = null;
            decimal? discountPercentage = null;

            if (request.createProductDTO.DiscountPercentage.HasValue)
            {
                discountPercentage = request.createProductDTO.DiscountPercentage.Value;
                discountPrice = request.createProductDTO.MarketPrice - (request.createProductDTO.MarketPrice * discountPercentage.Value / 100);
                discountPrice = Math.Max(0, discountPrice.Value);
            }
         


            // Create the new Product item
            var product = new Product
            {            
                CategoryId = category.Id,
                SubCategoryId = subCategory?.Id,
                SubSubCategoryId = subSubCategory?.Id,
                Name = request.createProductDTO.Name,
                Slug = request.createProductDTO.Slug,
                Description = request.createProductDTO.Slug,
                MarketPrice = request.createProductDTO.MarketPrice,
                CostPrice = request.createProductDTO.CostPrice,
                DiscountPrice = discountPrice,
                DiscountPercentage = discountPercentage,
                StockQuantity = request.createProductDTO.StockQuantity,
                Sku = request.createProductDTO.Sku,
                Weight = request.createProductDTO.Weight,
                Reviews = request.createProductDTO.Reviews,
                Rating = request.createProductDTO.Rating,
                Dimensions = request.createProductDTO.Dimensions,
                Category = category,
                SubCategory = subCategory,
                SubSubCategory = subSubCategory

            };

            // Add the product to the repository
            await _productRepository.AddAsync(product,cancellationToken);
            await _productRepository.SaveChangesAsync(cancellationToken);

            // Map to the Dto and return success
            return Result<ProductDTO>.Success(product.ToDTO(), "Product item created successfully");
        }
    }

}
