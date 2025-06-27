using Application.Dto.UserDTOs;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Queries.UserQuery;
using Application.Interfaces.Services;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.Authentication.Module
{
    public class AdminMod : CarterModule
    {
        public AdminMod() : base("")
        {
            WithTags("Admin");
            IncludeInOpenApi();

        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("/admin");

            app.MapPut("/updateUserRole", async (
                [FromQuery] int UserId,
                [FromForm] UserRoles Role,
                ISender mediator
                     ) =>
                            {
                                var command = new UpdateUserRoleCommand(UserId, Role);
                                var result = await mediator.Send(command);

                                if (!result.Succeeded)
                                {
                                    return Results.BadRequest(new { result.Message, result.Data });
                                }

                                return Results.Ok(new { result.Message, result.Data });

                            })
                /*.RequireAuthorization()*/
                            .DisableAntiforgery()
                              .Accepts<UpdateUserRoleCommand>("multipart/form-data")
                              .Produces<UpdateUserRoleDTO>(StatusCodes.Status200OK)
                             .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);


            app.MapGet("getUserByRole", async (
             ISender mediator,
              ICurrentUserService currentUserService,
            [FromQuery] int PageNumber = 1,
            [FromQuery] int PageSize = 10,
            [FromQuery] UserRoles Role = UserRoles.User
            ) =>
            {
                // Only allow Admin or SuperAdmin
                var userRole = currentUserService.Role;
                if (string.IsNullOrEmpty(userRole) ||
                    !(userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                      userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.Unauthorized();
                }

                var command = new GetUserByRoleQuery(PageNumber, PageSize, Role);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Data });
                }

                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization();
            


        }


    }
}
