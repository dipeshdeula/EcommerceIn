using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.ProductFeat.DeleteCommands
{
    public record HardDeleteProductCommand(int ProductId) : IRequest<Result<ProductDTO>>;

    public class HardDeleteProductCommandHandler(IProductRepository _productRepository) : IRequestHandler<HardDeleteProductCommand, Result<ProductDTO>>
        {

        public async Task<Result<ProductDTO>> Handle(HardDeleteProductCommand request, CancellationToken cancellationToken)
        {
             await _productRepository.HardDeleteProductAsync(request.ProductId, cancellationToken);          
            return Result<ProductDTO>.Success(null, "Product hard deleted successfully");


        }
    }


}
