using Application.Common;
using Application.Dto.AddressDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AddressFeat.Commands
{
    public record AddressCommand(
        int UserId,
        AddAddressDTO addressDTO
        ) : IRequest<Result<AddressDTO>>;

    public class AddressCommandHandler : IRequestHandler<AddressCommand, Result<AddressDTO>>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly IUserRepository _userRepository;

        public AddressCommandHandler(IAddressRepository addressRepository, IUserRepository userRepository)
        {
            _addressRepository = addressRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<AddressDTO>> Handle(AddressCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return Result<AddressDTO>.Failure("User doesn't exist!");
                }

                // Check if the address already exists
                var existingAddress = user.Addresses.FirstOrDefault(a =>
                    a.Label == request.addressDTO.Label &&
                    a.Street == request.addressDTO.Street &&
                    a.City == request.addressDTO.City &&
                    a.Province == request.addressDTO.Province &&
                    a.PostalCode == request.addressDTO.PostalCode &&
                    a.Latitude == Convert.ToDouble(request.addressDTO.Latitude) &&
                    a.Longitude == Convert.ToDouble(request.addressDTO.Longitude) 
                    );

                if (existingAddress != null)
                {
                    return Result<AddressDTO>.Failure("Address already exists!");
                }

                var address = new Address
                {
                    UserId = user.Id,
                    Label = request.addressDTO.Label,
                    Street = request.addressDTO.Street,
                    City = request.addressDTO.City,
                    Province = request.addressDTO.Province,
                    PostalCode = request.addressDTO.PostalCode,
                    Latitude = Convert.ToDouble(request.addressDTO.Latitude),
                    Longitude = Convert.ToDouble(request.addressDTO.Longitude),
                    
                };

                user.Addresses.Add(address);

                await _addressRepository.AddAsync(address, cancellationToken);
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                return Result<AddressDTO>.Success(address.ToDTO(), "Address added successfully");
            }
            catch (Exception ex)
            {
                return Result<AddressDTO>.Failure("Address registration failed", new[] { ex.Message });
            }
        }
    }
}

