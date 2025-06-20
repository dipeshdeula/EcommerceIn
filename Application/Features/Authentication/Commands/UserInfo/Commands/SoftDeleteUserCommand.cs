using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Commands.UserInfo.Commands
{
    public record SoftDeleteUserCommand(int UserId) : IRequest<Result<User>>;

    public class SoftDeleteUserCommandHandler : IRequestHandler<SoftDeleteUserCommand, Result<User>>
    {
        private readonly IUserRepository _userRepository;

        public SoftDeleteUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<User>> Handle(SoftDeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<User>.Failure("User not found");
            }

            await _userRepository.SoftDeleteAsync(user, cancellationToken);
            return Result<User>.Success(user, "User soft deleted successfully");
        }
    }
}
