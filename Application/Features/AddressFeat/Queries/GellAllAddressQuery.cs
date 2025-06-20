using Application.Common;
using Application.Dto.AddressDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.AddressFeat.Queries
{
    public record GellAllAddressQuery(int PageNumber, int PageSize) : IRequest<Result<IEnumerable<AddressDTO>>>;

    public class GetAllAddressQueryHandler : IRequestHandler<GellAllAddressQuery, Result<IEnumerable<AddressDTO>>>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ILogger<GellAllAddressQuery> _logger;

        public GetAllAddressQueryHandler(IAddressRepository addressRepository, ILogger<GellAllAddressQuery> logger)
        {
            _addressRepository = addressRepository;
            _logger = logger;
            
        }

        public async Task<Result<IEnumerable<AddressDTO>>> Handle(GellAllAddressQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all address with pagination");

            var addresses = await _addressRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(address => address.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize,
                cancellationToken:cancellationToken
                );

            var addressDTOs = addresses.Select(ad => ad.ToDTO()).ToList();

            return Result<IEnumerable<AddressDTO>>.Success(addressDTOs, "Address fetched successfully!");

        }
    }

} 
    

