using Application.Common;
using Application.Dto.UserDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Authentication.Queries.UserQuery
{
    public record GetUserByRoleQuery(
        int PageNumber,
        int PageSize,
        UserRoles Role
        ) : IRequest<Result<IEnumerable<UserDTO>>>;

    public class GetUserByRoleQueryHandler : IRequestHandler<GetUserByRoleQuery, Result<IEnumerable<UserDTO>>>
    {
        private readonly IUserRepository _userRepository;
        public GetUserByRoleQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            
        }
        public async Task<Result<IEnumerable<UserDTO>>> Handle(GetUserByRoleQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync(
                            predicate: u => u.Role == request.Role && !u.IsDeleted,
                            skip: (request.PageNumber - 1) * request.PageSize,
                            take: request.PageSize,
                            includeDeleted: false,
                            cancellationToken:cancellationToken
            );
            var userDto = users.Select(u => u.ToDTO()).ToList();

            return Result<IEnumerable<UserDTO>>.Success(userDto, "Users fetched successfully.");
        }
    }

}
