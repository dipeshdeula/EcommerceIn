using Application.Common;
using Application.Dto;
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
        int Id,
        string Label,
        string Street,
        string City,
        string Province,
        string PostalCode,
        string Latitude,
        string Longitude,
        bool IsDefault) : IRequest<Result<AddressDTO>>;

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
                var user = await _userRepository.FindByIdAsync(request.Id);
                if (user == null)
                {
                    return Result<AddressDTO>.Failure("User doesn't exist!");
                }

                // Check if the address already exists
                var existingAddress = user.Addresses.FirstOrDefault(a =>
                    a.Label == request.Label &&
                    a.Street == request.Street &&
                    a.City == request.City &&
                    a.Province == request.Province &&
                    a.PostalCode == request.PostalCode &&
                    a.Latitude == Convert.ToDouble(request.Latitude) &&
                    a.Longitude == Convert.ToDouble(request.Longitude) &&
                    a.IsDefault == request.IsDefault);

                if (existingAddress != null)
                {
                    return Result<AddressDTO>.Failure("Address already exists!");
                }

                var address = new Address
                {
                    UserId = user.Id,
                    Label = request.Label,
                    Street = request.Street,
                    City = request.City,
                    Province = request.Province,
                    PostalCode = request.PostalCode,
                    Latitude = Convert.ToDouble(request.Latitude),
                    Longitude = Convert.ToDouble(request.Longitude),
                    IsDefault = request.IsDefault
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

