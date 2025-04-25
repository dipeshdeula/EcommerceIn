using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.StoreAddressFeat.Commands
{
    public record CreateStoreAddressCommand( 
        int StoreId,
        string Street,
        string City,
        string Province,
        string PostalCode,
        double Latitude,
        double Longitude) : IRequest<Result<StoreAddressDTO>>;

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
                Street = request.Street,
                City = request.City,
                Province = request.Province,
                PostalCode = request.PostalCode,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            await _storeAddressRepository.AddAsync(storeAddress);

            return Result<StoreAddressDTO>.Success(storeAddress.ToDTO(),"Store address created successfylly");

        }
    }

}
