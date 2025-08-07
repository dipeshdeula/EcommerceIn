using Application.Dto.LocationDTOs;
using Application.Features.LocationFeat.Commands;
using Application.Features.LocationFeat.Queries;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.LocationFeat.Module
{
    public class LocationMod : CarterModule
    {
        public LocationMod() : base("location")
        {
            WithTags("Location");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var locationGroup = app.MapGroup("/location");

            // Validate location (GPS or IP)
            app.MapPost("/validate", async (
                [FromBody] LocationRequestDTO request,
                [FromQuery] int? userId,
                [FromServices] ILocationService locationService,
                [FromServices] ISender mediator) =>
            {
                var query = new ValidateLocationQuery(request, userId);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("ValidateLocation")
            .WithSummary("Validate user location and check service availability")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // Validate access by IP (for initial app access)
            app.MapGet("/validate-access", async (
                [FromQuery] string? ipAddress,
                [FromServices] ILocationService locationService,
                HttpContext context) =>
            {
                var clientIP = ipAddress ?? context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var result = await locationService.ValidateAccessByIPAsync(clientIP);

                return Results.Ok(result);
            })
            .WithName("ValidateAccess")
            .WithSummary("Validate app access based on IP location")
            .Produces<object>(StatusCodes.Status200OK);

            //  Get IP location
            app.MapGet("/ip-location", async (
                [FromQuery] string? ipAddress,
                [FromServices] ILocationService locationService,
                HttpContext context) =>
            {
                var clientIP = ipAddress ?? context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var result = await locationService.GetLocationFromIPAsync(clientIP);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("GetIPLocation")
            .WithSummary("Get location information from IP address")
            .Produces<object>(StatusCodes.Status200OK);

            app.MapPost("/save-location", async (
                [FromBody] LocationRequestDTO request,
                [FromQuery] int userId,
                [FromServices] ISender mediator) =>
            {
                var command = new SaveUserLocationCommand(userId, request);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, Data = "Location saved successfully" });
            })
            .RequireAuthorization()
            .WithName("SaveUserLocation")
            .WithSummary("Save user's current location")
            .Produces<object>(StatusCodes.Status200OK);


            // Find nearby stores
            app.MapGet("/nearby-stores", async (
                [FromServices] ILocationService locationService,
                [FromQuery] double latitude,
                [FromQuery] double longitude,
                [FromQuery] double radiusKm = 10
                ) =>
            {
                var result = await locationService.FindNearbyStoresAsync(latitude, longitude, radiusKm);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("FindNearbyStores")
            .WithSummary("Find stores near specified location")
            .Produces<object>(StatusCodes.Status200OK);

            //  Get service areas
            app.MapGet("/service-areas", async (
                [FromServices] ISender mediator,
                [FromQuery] bool activeOnly = true
                ) =>
            {
                var query = new GetServiceAreasQuery(activeOnly);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("GetServiceAreas")
            .WithSummary("Get all available service areas")
            .Produces<object>(StatusCodes.Status200OK);

            //  Reverse geocoding
            app.MapGet("/reverse-geocode", async (
                [FromQuery] double latitude,
                [FromQuery] double longitude,
                [FromServices] ILocationService locationService) =>
            {
                var result = await locationService.ReverseGeocodeAsync(latitude, longitude);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("ReverseGeocode")
            .WithSummary("Convert coordinates to address")
            .Produces<object>(StatusCodes.Status200OK);

            //  Forward geocoding
            app.MapGet("/forward-geocode", async (
                [FromQuery] string address,
                [FromServices] ILocationService locationService) =>
            {
                var result = await locationService.ForwardGeocodeAsync(address);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("ForwardGeocode")
            .WithSummary("Convert address to coordinates")
            .Produces<object>(StatusCodes.Status200OK);
        }


    }
}
