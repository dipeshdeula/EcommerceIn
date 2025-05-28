using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Authentication.Queries.UserQuery
{
    public record GetAllUsersQuery(int PageNumber, int PageSize) : IRequest<Result<IEnumerable<UserDTO>>>;

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserDTO>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetAllUsersQueryHandler> _logger;

        public GetAllUsersQueryHandler(IUserRepository userRepository, ILogger<GetAllUsersQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<UserDTO>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all users with pagination");

            var users = await _userRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(user => user.CreatedAt),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize
            );
            var userDTOs = users.Select(u => u.ToDTO()).ToList();


            return Result<IEnumerable<UserDTO>>.Success(userDTOs, "Users fetched successfully");
        }
    }
}
