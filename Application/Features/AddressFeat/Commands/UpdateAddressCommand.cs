using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.AddressFeat.Commands
{
    public record UpdateAddressCommand(
        int Id,
        string? Label,
        string? Street,
        string? City,
        string? Province,
        string? PostalCode,
        double? Latitude,
        double? Longitude

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
            var address = await _addressRepository.FindByIdAsync(request.Id);
            if (address == null)
                return Result<AddressDTO>.Failure($"Address with id : {request.Id} is not found");

            address.Label = request.Label ?? address.Label;
            address.Street = request.Street ?? address.Street;
            address.City = request.City ?? address.City;
            address.Province = request.Province ?? address.Province;
            address.PostalCode = request.PostalCode ?? address.PostalCode;
            address.Latitude = request.Latitude ?? address.Latitude;
            address.Longitude = request.Longitude ?? address.Longitude;

            await _addressRepository.UpdateAsync(address, cancellationToken);

            return Result<AddressDTO>.Success(address.ToDTO(), $"Address with id : {request.Id} is updated successfully");



        }
    }
}
