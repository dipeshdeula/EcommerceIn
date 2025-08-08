using Application.Common;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.ShippingFeat.Queries
{
    public record GetShippingByIdQuery(int Id) : IRequest<Result<ShippingDTO>>;

    public class GetShippingByIdQueryHandler : IRequestHandler<GetShippingByIdQuery, Result<ShippingDTO>>
    {
        private readonly IShippingRepository _shippingRepository;

        public GetShippingByIdQueryHandler(IShippingRepository shippingRepository)
        {
            _shippingRepository = shippingRepository;
        }

        public async Task<Result<ShippingDTO>> Handle(GetShippingByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = await _shippingRepository.GetAsync(
                    predicate: s => s.Id == request.Id && !s.IsDeleted,
                    includeProperties: "CreatedByUser,LastModifiedByUser",
                    includeDeleted: false,
                    cancellationToken: cancellationToken
                );

                if (configuration == null)
                {
                    return Result<ShippingDTO>.Failure("Shipping configuration not found");
                }

                var result = configuration.ToShippingDTO();
                return Result<ShippingDTO>.Success(result, "Shipping configuration retrieved successfully");
            }
            catch (Exception ex)
            {
                return Result<ShippingDTO>.Failure($"Error retrieving configuration: {ex.Message}");
            }
        }
    }
}