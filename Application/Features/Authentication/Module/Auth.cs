using Application.Features.Authentication.Commands;
using Application.Features.Authentication.Commands.UserInfo.Commands;
using Application.Features.Authentication.Otp.Commands;
using Application.Features.Authentication.Queries.Login;
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
    public class Auth : CarterModule
    {
        public Auth():base("")
        {
            WithTags("Authentication");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("auth");

            app.MapPost("/register", ([FromServices] ISender mediator, RegisterCommand command) =>
            {
                return mediator.Send(command);
            });

            app.MapPost("/verify-otp", async ([FromBody] VerifyOtpCommand command, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });

            });

            app.MapPost("/login", async ([FromBody] LoginQuery query, [FromServices] ISender mediator) =>
            {
                return await mediator.Send(query);
            });

            app.MapGet("/getUsers", async([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
                {
                var result = await mediator.Send(new GetAllUsersQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getUsers/{id:int}", async (int id, [FromServices] ISender mediator )=>
            {
                var result = await mediator.Send(new GetUsersQueryById(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("upload", async (ISender mediator, IFormFile file, int userId)=>
            {

                var comand = new UploadImageCommand(file, userId);
                var result = await mediator.Send(comand);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            }) .DisableAntiforgery()
                .Accepts<IFormFile>("multipart/form-data")
                 .Produces<string>(StatusCodes.Status200OK)
                 .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapPut("/updateUser/{id:int}", async (int id, [FromBody] UsersUpdateCommand command, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(command with { Id = id });
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/softDeleteUser/{id:int}", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new SoftDeleteUserCommand(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            });

            app.MapDelete("/hardDeleteUser/{id:int}", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new HardDeleteUserCommand(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            });

            app.MapPut("/undeleteUser/{id:int}", async (int id, [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(new UnDeleteUserCommnad(id));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            });


        }

        }
 }

