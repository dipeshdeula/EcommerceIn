using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductFeat.DeleteCommands
{
    public record UnDeleteProductCommand(int ProductId) : IRequest<Result<ProductDTO>>;

    public class UnDeleteProductCommandHandler(IProductRepository _productRepository) : IRequestHandler<UnDeleteProductCommand, Result<ProductDTO>>
    {
        public async Task<Result<ProductDTO>> Handle(UnDeleteProductCommand request, CancellationToken cancellationToken)
        {
            var success = await _productRepository.UndeleteProductAsync(request.ProductId, cancellationToken);
            if (!success)
            {
                return Result<ProductDTO>.Failure("Product not found");
            }
            return Result<ProductDTO>.Success(null, "Product undeleted successfully");
        }
    }



}
