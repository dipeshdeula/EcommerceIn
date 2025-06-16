using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductFeat.Queries
{
    public record GetProductByIdQuery(int productId, int PageNumber, int PageSize) : IRequest<Result<ProductDTO>>;

    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<GetProductByIdQuery> _logger;

        public GetProductByIdQueryHandler(IProductRepository productRepository, ILogger<GetProductByIdQuery> logger)
        {
            _productRepository = productRepository;
            _logger = logger;

        }
        public async Task<Result<ProductDTO>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.Queryable
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.productId);
            
            if (product == null)
            {
                return Result<ProductDTO>.Failure($"Product Id {request.productId} is not found");

            }
            return Result<ProductDTO>.Success(product.ToDTO(), $"Product id {request.productId} is fetched successfully");
        }
    }

}
