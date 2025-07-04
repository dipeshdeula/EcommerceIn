﻿using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.StoreAddressFeat.Commands
{
    public record UpdateStoreAddressCommand(
        int StoreId, UpdateStoreAddressDTO updateStoreAddressDTO) : IRequest<Result<StoreAddressDTO>>;

    public class UpdateStoreAddressCommandHandler : IRequestHandler<UpdateStoreAddressCommand, Result<StoreAddressDTO>>
    {
        private readonly IStoreAddressRepository _storeAddressRepository;

        public UpdateStoreAddressCommandHandler(IStoreAddressRepository storeAddressRepository)
        {
            _storeAddressRepository = storeAddressRepository;
        }

        public async Task<Result<StoreAddressDTO>> Handle(UpdateStoreAddressCommand request, CancellationToken cancellationToken)
        {
            // Fetch the store address by StoreId
            var storeAddress = await _storeAddressRepository.GetQueryable()
                .FirstOrDefaultAsync(sa => sa.StoreId == request.StoreId, cancellationToken);

            if (storeAddress == null)
            {
                return Result<StoreAddressDTO>.Failure("Store address not found for the given Store ID");
            }

            // Update the store address properties
            storeAddress.Street = request.updateStoreAddressDTO.Street ?? storeAddress.Street;
            storeAddress.City = request.updateStoreAddressDTO.City ?? storeAddress.City;
            storeAddress.Province = request.updateStoreAddressDTO.Province ?? storeAddress.Province;
            storeAddress.PostalCode = request.updateStoreAddressDTO.PostalCode ?? storeAddress.PostalCode;
            storeAddress.Latitude = request.updateStoreAddressDTO.Latitude ?? storeAddress.Latitude;
            storeAddress.Longitude = request.updateStoreAddressDTO.Longitude ?? storeAddress.Longitude ;

            // Save changes to the database
            await _storeAddressRepository.UpdateAsync(storeAddress, cancellationToken);
            await _storeAddressRepository.SaveChangesAsync(cancellationToken);

            // Map the updated address to DTO
            var storeAddressDTO = storeAddress.ToDTO();
            return Result<StoreAddressDTO>.Success(storeAddressDTO, "Store address updated successfully");
        }
    }
}

