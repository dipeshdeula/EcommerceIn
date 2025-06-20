using Application.Common;
using Application.Common.Helper;
using Application.Dto.UserDTOs;
using Application.Extension;
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
    public record UsersUpdateCommand(
        int Id, UpdateUserDTO updateUser
        ) : IRequest<Result<UserDTO>>;

    public class UsersUpdateCommandHandler : IRequestHandler<UsersUpdateCommand, Result<UserDTO>>
    {
        private readonly IUserRepository _userRepository;
        public UsersUpdateCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<Result<UserDTO>> Handle(UsersUpdateCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(request.Id);
            if (user is null)
            {
                return Result<UserDTO>.Failure("User not found");
            }
            user.Name = request.updateUser.Name ?? user.Name;
            user.Email = request.updateUser.Email ?? user.Email;
            if (!string.IsNullOrEmpty(request.updateUser.Password))
            {
                user.Password = PasswordHelper.HashPassword(request.updateUser.Password); // Hash the password
            }
            user.Contact = request.updateUser.Contact ?? user.Password;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            return Result<UserDTO>.Success(user.ToDTO(), "User updated successfully");
        }
    }

}
