using Application.Common;
using Application.Dto.UserDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Commands.UserInfo.Commands
{
    public record UpdateUserRoleCommand(
        int UserId,
        UserRoles Role
        ) : IRequest<Result<string>>;

    public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;

        public UpdateUserRoleCommandHandler(
            IUserRepository userRepository,
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Result<string>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            // Get role from current user service
            /*var role = _currentUserService.Role;
            if (string.IsNullOrEmpty(role) ||
                !(role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                  role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                return Result<string>.Failure("You are not authorized to update user roles.");
            }*/

            var checkUser = await _userRepository.FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted);

            if (checkUser == null)
            {
                return Result<string>.Failure("User not found");
            }

            checkUser.Role = request.Role;

            await _userRepository.UpdateAsync(checkUser, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return Result<string>.Success($"User with email {checkUser.Email} ,role has been updated to {checkUser.Role} ");
        }
    }
}
