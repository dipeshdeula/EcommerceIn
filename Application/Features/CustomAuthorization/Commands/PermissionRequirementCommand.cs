using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CustomAuthorization.Commands
{
    public class PermissionRequirementCommand : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirementCommand(string permission) => Permission = permission;
    }

    public class PermissionRequirementCommandHandler : AuthorizationHandler<PermissionRequirementCommand>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirementCommand requirement)
        {
            if (context.User.HasClaim("Permission", requirement.Permission))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
