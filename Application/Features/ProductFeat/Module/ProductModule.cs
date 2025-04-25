using Application.Dto;
using Application.Features.ProductFeat.Queries;
using Carter;
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

namespace Application.Features.ProductFeat.Module
{
    public class ProductModule : CarterModule
    {
        public ProductModule() : base("api/products") { }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/nearby", async (ISender mediator,double lat, double lon, double radius,int skip = 0,int take = 10) =>
            {
                var query = new GetNearbyProductsQuery(lat, lon, radius,skip,take);
                var result = await mediator.Send(query);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            }).WithName("GetNearbyProducts")
            .Produces<IEnumerable<NearbyProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");
        }
    }
}
