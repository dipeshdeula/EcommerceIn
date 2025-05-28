using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AddressFeat.Queries
{
    public record GetAddressByUserId(int UserId, int PageNumber, int PageSize) : IRequest<Result<IEnumerable<AddressDTO>>>;

    public class GetAddressByUserIdHandler : IRequestHandler<GetAddressByUserId, Result<IEnumerable<AddressDTO>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetAddressByUserIdHandler> _logger;

        public GetAddressByUserIdHandler(IUserRepository userRepository, ILogger<GetAddressByUserIdHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<AddressDTO>>> Handle(GetAddressByUserId request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching addresses for user with ID {UserId}", request.UserId);

            var user = await _userRepository.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<IEnumerable<AddressDTO>>.Failure("User not found");
            }

            var addresses = user.Addresses
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => a.ToDTO())
                .ToList();

            return Result<IEnumerable<AddressDTO>>.Success(addresses, "User addresses fetched successfully");
        }
    }
}


