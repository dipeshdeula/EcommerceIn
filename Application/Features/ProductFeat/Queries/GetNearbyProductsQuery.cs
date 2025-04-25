using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.ProductFeat.Queries
{
    public record GetNearbyProductsQuery(double Latitude, double Longitude, double RadiusKm,int skip,int take) : IRequest<Result<IEnumerable<NearbyProductDto>>>;

    public class GetNearbyProductQueryHandler : IRequestHandler<GetNearbyProductsQuery, Result<IEnumerable<NearbyProductDto>>>
    {
        private readonly IProductStoreRepository _productStoreRepository;

        public GetNearbyProductQueryHandler(IProductStoreRepository productStoreRepository)
        {
            _productStoreRepository = productStoreRepository;
        }


        public async Task<Result<IEnumerable<NearbyProductDto>>> Handle(GetNearbyProductsQuery request, CancellationToken cancellationToken)
        {

            var nearbyProducts = await _productStoreRepository.GetNearbyProductsAsync(
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                request.skip,
                request.take);

            return Result<IEnumerable<NearbyProductDto>>.Success(nearbyProducts, "Nearby products retrieved successfully.");
        }
    }
}
