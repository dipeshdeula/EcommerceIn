using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Commands.UserInfo.Commands
{
    public record HardDeleteUserCommand(int UserId) : IRequest<Result<User>>;

    public class HardDeleteUserCommandHandler(IUserRepository _userRepository) : IRequestHandler<HardDeleteUserCommand, Result<User>>
    {
        public async Task<Result<User>> Handle(HardDeleteUserCommand request, CancellationToken cancellationToken)
        {
           await _userRepository.HardDeleteUserAsync(request.UserId, cancellationToken);

            return Result<User>.Success(null, "User hard deleted successfully");
        }
    }
}
