using Application.Dto.UserDTOs;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Queries.UserQuery;
using Application.Features.Authentication.UploadImage.Commands;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.Authentication.Module
{
    public class UserMod : CarterModule
    {
        public UserMod() : base("")
        {
            WithTags("Auth User");
            IncludeInOpenApi();
            RequireAuthorization();
            
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("/user");

            app.MapGet("/getUsers", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdmin");

            app.MapGet("/getUsersById", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new GetUsersQueryById(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("upload", async (ISender mediator, IFormFile file, int userId) =>
            {

                var comand = new UploadImageCommand(file, userId);
                var result = await mediator.Send(comand);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            }).DisableAntiforgery()
                .Accepts<IFormFile>("multipart/form-data")
                 .Produces<string>(StatusCodes.Status200OK)
                 .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapPut("/updateUser", async ([FromQuery] int id, UpdateUserDTO updateUserDto, [FromServices] ISender mediator) =>
            {
                var command = new UsersUpdateCommand(id, updateUserDto);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/softDeleteUser", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new SoftDeleteUserCommand(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            }).RequireAuthorization("RequireAdmin");

            app.MapDelete("/hardDeleteUser", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new HardDeleteUserCommand(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            }).RequireAuthorization("RequireAdmin");

            app.MapDelete("/undeleteUser", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new UnDeleteUserCommnad(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            }).RequireAuthorization("RequireAdmin");
        }
    }
}
