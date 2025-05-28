using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.ProductFeat.DeleteCommands
{
    public record SoftDeleteProductCommand(
        int ProductId
        ) : IRequest<Result<ProductDTO>>;

    public class SoftDeleteProductCommandHandler : IRequestHandler<SoftDeleteProductCommand, Result<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        public SoftDeleteProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
            
        }
        public async Task<Result<ProductDTO>> Handle(SoftDeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<ProductDTO>.Failure("Product not found");
            }

            await _productRepository.SoftDeleteProductAsync(request.ProductId, cancellationToken);
            return Result<ProductDTO>.Success(product.ToDTO(), "Product soft deleted successfully");

        }
    }
}
