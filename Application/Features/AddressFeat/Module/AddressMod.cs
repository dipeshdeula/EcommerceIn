using Application.Features.AddressFeat.Commands;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Application.Dto;
using Application.Features.AddressFeat.Queries;

namespace Application.Features.AddressFeat.Module
{
    public class AddressMod : CarterModule
    {
        public AddressMod() : base("")
        {
            WithTags("Address");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("address");

            app.MapPost("/add", async ([FromServices] ISender mediator, [FromQuery] int userId, [FromBody] AddressCommand command) =>
            {
                var result = await mediator.Send(command with { Id = userId });
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAddresses", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) => 
            {
                var result = await mediator.Send(new GellAllAddressQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAddressessByUserId", async ([FromServices] ISender mediator, [FromQuery] int UserId, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAddressByUserId(UserId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });
        }
    }
}

