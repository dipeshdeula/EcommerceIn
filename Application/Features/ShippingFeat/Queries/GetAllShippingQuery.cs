using Application.Common;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.ShippingFeat.Queries
{
    public record GetAllShippingQuery(
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<Result<List<ShippingDTO>>>;

    public class GetAllShippingQueryHandler : IRequestHandler<GetAllShippingQuery, Result<List<ShippingDTO>>>
    {
        private readonly IShippingRepository _shippingRepository;

        public GetAllShippingQueryHandler(IShippingRepository shippingRepository)
        {
            _shippingRepository = shippingRepository;
        }

        public async Task<Result<List<ShippingDTO>>> Handle(GetAllShippingQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var configurations = await _shippingRepository.GetWithIncludesAsync(
                    predicate: s => !s.IsDeleted,
                    orderBy: q => q.OrderByDescending(s => s.IsDefault).ThenBy(s => s.Name),
                    s => s.CreatedByUser,
                    s => s.LastModifiedByUser
                );

                var result = configurations.Select(c => c.ToShippingDTO()).ToList();
                return Result<List<ShippingDTO>>.Success(result, "Shipping configurations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Result<List<ShippingDTO>>.Failure($"Error retrieving configurations: {ex.Message}");
            }
        }
    }
}