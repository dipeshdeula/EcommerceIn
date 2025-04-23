using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.ProductFeat.Queries
{
    public record GetNearbyProductsQuery(double Latitude, double Longitude, double RadiusKm) : IRequest<Result<IEnumerable<NearbyProductDto>>>;

    public class GetNearbyProductQueryHandler : IRequestHandler<GetNearbyProductsQuery, Result<IEnumerable<NearbyProductDto>>>
    {
        private readonly IProductStoreRepository _productStoreRepository;

        public GetNearbyProductQueryHandler(IProductStoreRepository productStoreRepository)
        {
            _productStoreRepository = productStoreRepository;
        }


        public async Task<Result<IEnumerable<NearbyProductDto>>> Handle(GetNearbyProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _productStoreRepository.GetNearbyProductsAsync(
                request.Latitude,
                request.Longitude,
                request.RadiusKm);

            return Result<IEnumerable<NearbyProductDto>>.Success(products, "Nearby products retrieved successfully.");
        }
    }
}
