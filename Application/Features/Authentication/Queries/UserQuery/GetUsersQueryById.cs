using Application.Common;
using Application.Dto.UserDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Queries.UserQuery
{
    public record GetUsersQueryById(int Id) : IRequest<Result<UserDTO>>;

    public class GetUsersQueryByIdHandler : IRequestHandler<GetUsersQueryById, Result<UserDTO>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUsersQueryByIdHandler> _logger;

        public GetUsersQueryByIdHandler(IUserRepository userRepository, ILogger<GetUsersQueryByIdHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<UserDTO>> Handle(GetUsersQueryById request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching user with ID {request.Id}");

            var user = await _userRepository.FirstOrDefaultAsync(x=>x.Id == request.Id && x.IsDeleted == false);
            if (user is null)
            {
                return Result<UserDTO>.Failure("User not found");
            }

            return Result<UserDTO>.Success(user.ToDTO(), "User fetched successfully");
        }
    }
}
