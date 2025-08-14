using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.ProductFeat.Commands
{
    public record UpdateProductCommand(
        int ProductId,
        int? CategoryId,
        int? SubCategoryId,
        int? SubSubCategoryId,
        UpdateProductDTO updateProductDto
        ) : IRequest<Result<ProductDTO>>;

    public class UpdateProudctComamndHandler : IRequestHandler<UpdateProductCommand, Result<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly ILogger<UpdateProductCommand> _logger;

        public UpdateProudctComamndHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ISubCategoryRepository subCategoryRepository,
            ISubSubCategoryRepository subSubCategoryRepository,
            ILogger<UpdateProductCommand> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _subCategoryRepository = subCategoryRepository;
            _subSubCategoryRepository = subSubCategoryRepository;
            _logger = logger;

        }

        public async Task<Result<ProductDTO>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<ProductDTO>.Failure("Product Id not found");
            }
            Category? category = null;
            SubCategory? subCategory = null;
            SubSubCategory? subSubCategory = null;

            if (request.CategoryId.HasValue)
            {
                category = await _categoryRepository.FindByIdAsync(request.CategoryId.Value);
                if (category == null)
                    return Result<ProductDTO>.Failure("Category not found");
            }

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



            product.Name = request.updateProductDto.Name ?? product.Name;
            product.Slug = request.updateProductDto.Slug ?? product.Slug;
            product.Description = request.updateProductDto.Description ?? product.Description;
            product.MarketPrice = request.updateProductDto.MarketPrice ?? product.MarketPrice;
            product.CostPrice = request.updateProductDto.CostPrice ?? product.CostPrice;
            product.StockQuantity = request.updateProductDto.StockQuantity ?? product.StockQuantity;
            product.Sku = request.updateProductDto.Sku ?? product.Sku;
            product.Weight = request.updateProductDto.Weight ?? product.Weight;
            product.Reviews = request.updateProductDto.Reviews ?? product.Reviews;
            product.Rating = request.updateProductDto.Rating ?? product.Rating;
            product.Dimensions = request.updateProductDto.Dimensions ?? product.Dimensions;
            product.CategoryId = request.CategoryId ?? product.CategoryId;
            product.SubCategoryId = request.SubCategoryId ?? product.SubCategoryId;
            product.SubSubCategoryId = request.SubSubCategoryId ?? product.SubSubCategoryId;

            // Discount logic
            if (request.updateProductDto.DiscountPercentage.HasValue)
            {
                var discountPercentage = request.updateProductDto.DiscountPercentage.Value;
                product.DiscountPercentage = discountPercentage;
                product.DiscountPrice = product.MarketPrice - (product.MarketPrice * discountPercentage / 100);
            }

            await _productRepository.UpdateAsync(product, cancellationToken);
            await _productRepository.SaveChangesAsync(cancellationToken);

            return Result<ProductDTO>.Success(product.ToDTO(), "Product updated successfully");
        }
    }

}
