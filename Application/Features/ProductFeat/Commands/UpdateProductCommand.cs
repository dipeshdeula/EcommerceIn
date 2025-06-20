using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.ProductFeat.Commands
{
    public record UpdateProductCommand(
        int ProductId,
        UpdateProductDTO updateProductDto
        ) : IRequest<Result<ProductDTO>>;

    public class UpdateProudctComamndHandler : IRequestHandler<UpdateProductCommand, Result<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<UpdateProductCommand> _logger;

        public UpdateProudctComamndHandler(IProductRepository productRepository, ILogger<UpdateProductCommand> logger)
        {
            _productRepository = productRepository;
            _logger = logger;

        }

        public async Task<Result<ProductDTO>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<ProductDTO>.Failure("Product Id not found");
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
