using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.DeleteCommands
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
