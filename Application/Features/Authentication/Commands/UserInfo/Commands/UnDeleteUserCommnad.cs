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
    public record UnDeleteUserCommnad(int UserId) : IRequest<Result<User>>;

    public class UnDeleteUserCommandHandler(IUserRepository _userRepository) : IRequestHandler<UnDeleteUserCommnad, Result<User>>
    {
        public async Task<Result<User>> Handle(UnDeleteUserCommnad request, CancellationToken cancellationToken)
        {
            var success = await _userRepository.UndeleteUserAsync(request.UserId, cancellationToken);
            if(!success)
            {
                return Result<User>.Failure("User not found");
            }
            return Result<User>.Success(null,"User undeleted successfully");
        }
    }

}
