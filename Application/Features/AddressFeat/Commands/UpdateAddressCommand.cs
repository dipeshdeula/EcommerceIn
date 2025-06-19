using Application.Common;
using Application.Dto.AddressDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.AddressFeat.Commands
{
    public record UpdateAddressCommand(
        UpdateAddressDTO updateAddressDto

        ) : IRequest<Result<AddressDTO>>;
    public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result<AddressDTO>>
    {
        private readonly IAddressRepository _addressRepository;
        public UpdateAddressCommandHandler(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;

        }
        public async Task<Result<AddressDTO>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
        {
            var address = await _addressRepository.FindByIdAsync(request.updateAddressDto.Id);
            if (address == null)
                return Result<AddressDTO>.Failure($"Address with id : {request.updateAddressDto.Id} is not found");

            address.Label = request.updateAddressDto.Label ?? address.Label;
            address.Street = request.updateAddressDto.Street ?? address.Street;
            address.City = request.updateAddressDto.City ?? address.City;
            address.Province = request.updateAddressDto.Province ?? address.Province;
            address.PostalCode = request.updateAddressDto.PostalCode ?? address.PostalCode;
            address.Latitude = request.updateAddressDto.Latitude ?? address.Latitude;
            address.Longitude = request.updateAddressDto.Longitude ?? address.Longitude;

            await _addressRepository.UpdateAsync(address, cancellationToken);

            return Result<AddressDTO>.Success(address.ToDTO(), $"Address with id : {request.updateAddressDto.Id} is updated successfully");



        }
    }
}
