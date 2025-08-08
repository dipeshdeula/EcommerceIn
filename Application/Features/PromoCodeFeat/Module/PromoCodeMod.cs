using Application.Common;
using Application.Common.Models;
using Application.Dto.PromoCodeDTOs;
using Application.Features.PromoCodeFeat.Commands;
using Application.Features.PromoCodeFeat.DeleteCommands;
using Application.Features.PromoCodeFeat.Queries;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PromoCodeFeat.Module
{
    public class PromoCodeModule : CarterModule
    {
        public PromoCodeModule() : base()
        {
            WithTags("PromoCode");
            IncludeInOpenApi();
            RequireRateLimiting("api");
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("/admin");

                     

            // Admin endpoints
            app.MapGet("/admin/all", async (
                [FromServices] IMediator mediator,
                [FromQuery] bool includeInactive = false,
                [FromQuery] bool includeExpired = false,
                [FromQuery] int ? categoryId = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 20
            )=>
            {
                var query = new GetAllPromoCodesQuery(includeInactive, includeExpired, categoryId, pageNumber, pageSize);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(result.Data);
            })
                .WithName("GetAllPromoCodes")
                .WithSummary("Get all promo codes (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces<PagedResult<PromoCodeDTO>>(StatusCodes.Status200OK);

            app.MapGet("/admin/{id:int}", async (
                int id,
               [FromServices] IMediator mediator)=>
        {
                var query = new GetPromoCodeByIdQuery(id);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.NotFound(new { result.Message });

                return Results.Ok(result.Data);
            })  .WithName("GetPromoCodeById")
                .WithSummary("Get promo code by ID (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces<PromoCodeDTO>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);

            app.MapPost("/admin/create", async (
                [FromBody] AddPromoCodeDTO request,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IUserRepository userRepository,
                [FromServices] IMediator mediator)=>
        {
                var userId = currentUserService.GetUserIdAsInt();
                var userInfo = await userRepository.FindByIdAsync(userId);
                if (userInfo == null)
                {
                    return Results.BadRequest("User not found");
                }

                var command = new CreatePromoCodeCommand(request, userInfo.Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Created($"/promo-codes/admin/{result.Data.Id}", result.Data);
            })   .WithName("CreatePromoCode")
                .WithSummary("Create new promo code (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces<PromoCodeDTO>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest);

            app.MapPut("/admin/{id:int}", async (
                 int id,
                [FromBody] UpdatePromoCodeDTO request,
                [FromServices] IMediator mediator)=>
        {
                // TODO: Get user ID from authentication context
                var userId = 1; // Replace with actual admin user ID from JWT

                var command = new UpdatePromoCodeCommand(id, request, userId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.NotFound(new { result.Message });

                return Results.Ok(result.Data);
            })  .WithName("UpdatePromoCode")
                .WithSummary("Update promo code (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces<PromoCodeDTO>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);

            app.MapPut("/admin/{id:int}/activate", async (
                int id,
                [FromServices] IUserRepository userRepository,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IMediator mediator)=>
        {

                var userId = currentUserService.GetUserIdAsInt();
                var userInfo = await userRepository.FindByIdAsync(userId);
                if (userInfo == null)
                {
                    return Results.BadRequest("User Id is not found");

                }

                var command = new ActivatePromoCodeCommand(id, userInfo.Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { message = "Promo code activated successfully" });
            })  .WithName("ActivatePromoCode")
                .WithSummary("Activate promo code (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces(StatusCodes.Status200OK);

            app.MapPut("/admin/{id:int}/deactivate", async (
                 [FromServices] ICurrentUserService currentUserservice,
                [FromServices] IUserRepository userRepository,
                int id,
                [FromServices] IMediator mediator)=>
        {
                var userId = currentUserservice.GetUserIdAsInt();

                var userInfo = await userRepository.FindByIdAsync(userId);
                if (userInfo == null)
                {
                    return Results.BadRequest("UserId not found");
                }

                var command = new DeactivatePromoCodeCommand(id, userInfo.Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { message = "Promo code deactivated successfully" });
            })  .WithName("DeactivatePromoCode")
                .WithSummary("Deactivate promo code (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces(StatusCodes.Status200OK);

            app.MapGet("/admin/{id:int}/usage", async (
                int id,
                [FromServices] IMediator mediator) =>
            {
                var query = new GetPromoCodeUsageQuery(id);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.NotFound(new { result.Message });

                return Results.Ok(result.Data);
            })   .WithName("GetPromoCodeUsage")
                .WithSummary("Get promo code usage history (Admin)")
                .RequireAuthorization("RequireAdmin")
                .Produces<List<PromoCodeUsageDTO>>(StatusCodes.Status200OK);

            app.MapGet("/{id:int}/softDelete", async (
               [FromQuery] int id,
               [FromServices] ISender mediator,
               ICurrentUserService currentUserService,
               IUserRepository userRepository
               ) =>{
                try
                {
                    var user = currentUserService.GetUserIdAsInt();
                    var userInfo = await userRepository.FindByIdAsync(user);
                    if (userInfo == null)
                    {
                        return Results.BadRequest("User not found");
                    }
                    
                    var command = new SoftDeletePromoCodeCommand(id, userInfo.Id);
                    var result = await mediator.Send(command);

                    if (!result.Succeeded)
                    {
                        return Results.BadRequest(new { result.Message, result.Errors });
                    }

                    return Results.Ok(new { result.Message, result.Data });

                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { ex.Message });
                }
               

            })
               .RequireAuthorization("RequireAdmin")
               .Produces<string>(StatusCodes.Status200OK);

            app.MapDelete("/{id:int}/unDelete", async (
                [FromQuery] int id,
                ISender mediator
                ) =>
            {
                var command = new UnDeletePromoCodeCommand(id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });

            })
                .RequireAuthorization("RequireAdmin")
                .Produces<string>(StatusCodes.Status200OK);

            app.MapGet("/{id:int}/hardDelete", async (
               [FromQuery] int id,
               [FromServices] ISender mediator,
               ICurrentUserService currentUserService,
               IUserRepository userRepository
               ) => {
                   try
                   {
                       var user = currentUserService.GetUserIdAsInt();
                       var userInfo = await userRepository.FindByIdAsync(user);
                       if (userInfo == null)
                       {
                           return Results.BadRequest("User not found");
                       }

                       var command = new HardDeletePromoCodeCommand(id, userInfo.Id);
                       var result = await mediator.Send(command);

                       if (!result.Succeeded)
                       {
                           return Results.BadRequest(new { result.Message, result.Errors });
                       }

                       return Results.Ok(new { result.Message, result.Data });

                   }
                   catch (Exception ex)
                   {
                       return Results.BadRequest(new { ex.Message });
                   }


               })
               .RequireAuthorization("RequireAdmin")
               .Produces<string>(StatusCodes.Status200OK);

        }         
    }
}