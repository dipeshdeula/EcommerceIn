using Application.Dto.CategoryDTOs;
using Application.Dto.UserDTOs;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.CategoryFeat.UpdateCommands;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            app = app.MapGroup("");

            app.MapPut("/updateUserRole", async (
                [FromQuery] int UserId,
                [FromForm] UserRoles Role,
                //[FromForm] UpdateUserRoleDTO updateUserRoleDto,
    ISender mediator
    ) =>
            {
                var command = new UpdateUserRoleCommand(UserId,Role);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Data });
                }

                return Results.Ok(new { result.Message, result.Data });

            }).RequireAuthorization()
            .DisableAntiforgery()
              .Accepts<UpdateUserRoleCommand>("multipart/form-data")
              .Produces<UpdateUserRoleDTO>(StatusCodes.Status200OK)
              .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
        }
    }
}
