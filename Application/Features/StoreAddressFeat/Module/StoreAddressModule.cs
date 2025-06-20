using Application.Dto.StoreDTOs;
using Application.Features.StoreAddressFeat.Commands;
using Application.Features.StoreAddressFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.StoreAddressFeat.Module
{
    public class StoreAddressModule : CarterModule
    {
        public StoreAddressModule() : base("")
        {
            WithTags("StoreAddress");
            IncludeInOpenApi();
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("StoreAddress");
            app.MapPost("/add", async ([FromServices] ISender mediator, 
                [FromQuery] int storeId,
                AddStoreAddressDTO addStoreAddressDto) =>
            {
                var command = new CreateStoreAddressCommand(storeId, addStoreAddressDto);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getStoreAddressByStoreId", async (int StoreId, ISender mediator) =>
            {
                var command = new GetStoreAddressByStoreIdQuery(StoreId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPut("/updateStoreAddress", async (
                [FromQuery] int StoreId, 
                [FromBody] UpdateStoreAddressDTO updateAddressStoreDto ,ISender mediator) =>
            {
                var command = new UpdateStoreAddressCommand(StoreId, updateAddressStoreDto);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

        }
    }
    
}
