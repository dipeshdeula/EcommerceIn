using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Queries.UserQuery
{
    public record GetUsersQueryById(int Id) : IRequest<Result<User>>;

    public class GetUsersQueryByIdHandler : IRequestHandler<GetUsersQueryById, Result<User>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUsersQueryByIdHandler> _logger;

        public GetUsersQueryByIdHandler(IUserRepository userRepository, ILogger<GetUsersQueryByIdHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<User>> Handle(GetUsersQueryById request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching user with ID {request.Id}");

            var user = await _userRepository.FindByIdAsync(request.Id);
            if (user is null)
            {
                return Result<User>.Failure("User not found");
            }

            return Result<User>.Success(user, "User fetched successfully");
        }
    }
}
