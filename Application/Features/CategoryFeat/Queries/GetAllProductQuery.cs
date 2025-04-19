using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllProductQuery (int PageNumber,int PageSize) : IRequest<Result<IEnumerable<ProductDTO>>>;

    public class GetAllProductQueryHandler : IRequestHandler<GetAllProductQuery, Result<IEnumerable<ProductDTO>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<GetAllProductQuery> _logger;
        public GetAllProductQueryHandler(IProductRepository productRepository, ILogger<GetAllProductQuery> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
            
        }
        public async Task<Result<IEnumerable<ProductDTO>>> Handle(GetAllProductQuery request, CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(product => product.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize
                );

            // Map products to DTOs
            var productDTOsa = products.Select(p => p.ToDTO()).ToList();

            return Result<IEnumerable<ProductDTO>>.Success(productDTOsa, "Product is fetched successfully");
        }
    }

}
