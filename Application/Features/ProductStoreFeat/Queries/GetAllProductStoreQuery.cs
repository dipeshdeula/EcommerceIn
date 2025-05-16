using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductStoreFeat.Queries
{
    public record GetAllProductStoreQuery (
        int PageNumber , int PageSize): IRequest<Result<IEnumerable<ProductStoreDTO>>>;

    public class GetAllProductStoreQueryHandler : IRequestHandler<GetAllProductStoreQuery, Result<IEnumerable<ProductStoreDTO>>>
    {
        private readonly IProductStoreRepository _productStoreRepository;
        public GetAllProductStoreQueryHandler(IProductStoreRepository productStoreRepository)
        {
            _productStoreRepository = productStoreRepository;            
        }
        public async Task<Result<IEnumerable<ProductStoreDTO>>> Handle(GetAllProductStoreQuery request, CancellationToken cancellationToken)
        {
            var productStore = await _productStoreRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(ps => ps.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize
                );

            var productStoreDTOs = productStore.Select(ps => ps.ToDTO()).ToList();

            return  Result<IEnumerable<ProductStoreDTO>>.Success(productStoreDTOs, "ProductStore fetched successfully");
        }
    }

}
