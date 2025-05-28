using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.AddressFeat.Commands
{
    public record HardDeleteAddressCommand (int Id): IRequest<Result<AddressDTO>>;

    public class HardDeleteAddressCommmandHandler : IRequestHandler<HardDeleteAddressCommand, Result<AddressDTO>>
    {
        private readonly IAddressRepository _addressRepository;
        public HardDeleteAddressCommmandHandler(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
            
        }
        public async Task<Result<AddressDTO>> Handle(HardDeleteAddressCommand request, CancellationToken cancellationToken)
        {
            var address = await _addressRepository.FindByIdAsync(request.Id);
            if (address == null)
                return Result<AddressDTO>.Failure($"Address with Id : {request.Id} is not found");

            await _addressRepository.HardDeleteAddressAsync(request.Id, cancellationToken);

            return Result<AddressDTO>.Success(address.ToDTO(),$"Address with Id: {request.Id} is deleted successfully");
        }
    }

}
