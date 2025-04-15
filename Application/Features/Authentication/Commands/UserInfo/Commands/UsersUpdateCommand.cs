using Application.Common;
using Application.Common.Helper;
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
    public record UsersUpdateCommand(int Id, string? name, string? email, string? password, string? contact) : IRequest<Result<User>>;

    public class UsersUpdateCommandHandler : IRequestHandler<UsersUpdateCommand, Result<User>>
    {
        private readonly IUserRepository _userRepository;
        public UsersUpdateCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<Result<User>> Handle(UsersUpdateCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(request.Id);
            if (user is null)
            {
                return Result<User>.Failure("User not found");
            }
            user.Name = request.name ?? user.Name;
            user.Email = request.email ?? user.Email;
            if (!string.IsNullOrEmpty(request.password))
            {
                user.Password = PasswordHelper.HashPassword(request.password); // Hash the password
            }
            user.Contact = request.contact ?? user.Password;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            return Result<User>.Success(user, "User updated successfully");
        }
    }

}
