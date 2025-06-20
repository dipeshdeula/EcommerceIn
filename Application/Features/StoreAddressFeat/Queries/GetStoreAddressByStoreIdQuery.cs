using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.StoreAddressFeat.Queries
{
    public record GetStoreAddressByStoreIdQuery(int StoreId) : IRequest<Result<StoreAddressDTO>>;

    public class GetStoreAddressByStoreIdQueryHandler : IRequestHandler<GetStoreAddressByStoreIdQuery, Result<StoreAddressDTO>>
    {
        private readonly IStoreRepository _storeRepository;

        public GetStoreAddressByStoreIdQueryHandler(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public async Task<Result<StoreAddressDTO>> Handle(GetStoreAddressByStoreIdQuery request, CancellationToken cancellationToken)
        {
            // Fetch the store with its address
            var store = await _storeRepository.GetQueryable()
                .Where(s => s.Id == request.StoreId)
                .Include(s => s.Address) // Include the Address navigation property
                .FirstOrDefaultAsync(cancellationToken);

            if (store == null)
            {
                return Result<StoreAddressDTO>.Failure("Store ID not found");
            }

            if (store.Address == null)
            {
                return Result<StoreAddressDTO>.Failure("Store address not found");
            }

            // Map the address to DTO
            var storeAddressDTO = store.Address.ToDTO();
            return Result<StoreAddressDTO>.Success(storeAddressDTO, "Store address fetched successfully");
        }
    }
}

