using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.StoreAddressFeat.Commands
{
    public record CreateStoreAddressCommand( 
        int StoreId,
       AddStoreAddressDTO addStoreAddressDto) : IRequest<Result<StoreAddressDTO>>;

    public class CreateStoreAddressCommandHandler : IRequestHandler<CreateStoreAddressCommand, Result<StoreAddressDTO>>
    {
        private readonly IStoreAddressRepository _storeAddressRepository;
        private readonly IStoreRepository _storeRepository;
        public CreateStoreAddressCommandHandler(IStoreAddressRepository storeAddressRepository, IStoreRepository storeRepository)
        {
            _storeAddressRepository = storeAddressRepository;
            _storeRepository = storeRepository;
        }

        public async Task<Result<StoreAddressDTO>> Handle(CreateStoreAddressCommand request, CancellationToken cancellationToken)
        {
            var store = await _storeRepository.FindByIdAsync(request.StoreId);
            if (store == null)
            {
                return Result<StoreAddressDTO>.Failure("Store doesn't exist!");
            }

            var storeAddress = new StoreAddress
            {
                StoreId = request.StoreId,
                Street = request.addStoreAddressDto.Street,
                City = request.addStoreAddressDto.City,
                Province = request.addStoreAddressDto.Province,
                PostalCode = request.addStoreAddressDto.PostalCode,
                Latitude = request.addStoreAddressDto.Latitude,
                Longitude = request.addStoreAddressDto.Longitude
            };

            await _storeAddressRepository.AddAsync(storeAddress);
            await _storeAddressRepository.SaveChangesAsync(cancellationToken);

            return Result<StoreAddressDTO>.Success(storeAddress.ToDTO(),"Store address created successfylly");

        }
    }

}
