using Application.Features.Authentication.Commands;
using Application.Features.Authentication.Otp.Commands;
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

            /*app.MapPost("/login", async ([FromBody] LoginQuery query, [FromServices] ISender mediator) =>
            {
                return await mediator.Send(query);
            });*/



        }
    }
}
