using Application.Common;
using Application.Dto.PaymentMethodDTOs;
using Application.Features.PaymentMethodFeat.Commands;
using Application.Features.PaymentMethodFeat.DeleteCommands;
using Application.Features.PaymentMethodFeat.Queries;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PaymentMethodFeat.Module
{
    public class PaymentMethodMod : CarterModule
    {
        public PaymentMethodMod() : base("")
        {
            WithTags("PaymentMethod");
            IncludeInOpenApi();
            
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("paymentMethod");

            app.MapPost("/create", async (
                [FromForm] string Name,
                [FromForm] PaymentMethodType Type,
                [FromForm] IFormFile File,
                ISender mediator
                ) =>
            {
                var command = new CreatePaymentMethodCommand(Name, Type, File);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            }).DisableAntiforgery()
            .Accepts<CreatePaymentMethodCommand>("multipart/form-data")
            .Produces<PaymentMethodDTO>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapGet("/getAllPaymentMethod", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllPaymentMethodQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPut("/updatePaymentMethod", async(
                [FromQuery] int Id,
                [FromForm] string ? Name,
                [FromForm] PaymentMethodType? Type,
                [FromForm] IFormFile? File, ISender mediator) =>
            {
                var command = new UpdatePaymentMethodCommand(Id, Name,Type, File);
                var result = await mediator.Send(command);
                if(!result.Succeeded)
                    return Results.BadRequest(new {result.Message, result.Errors});
                return Results.Ok(new { result.Message,result.Data});


            }).DisableAntiforgery()
            .Accepts<UpdatePaymentMethodCommand>("multipart/form-data")
            .Produces<PaymentMethodDTO>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapDelete("/softDeletePaymentMethod", async (ISender mediator, int Id) =>
            {
                var command = new SoftDeletePaymentMethodCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/unDeletePaymentMethod", async (ISender mediator, int Id) =>
            {
                var command = new UnDeletePaymentMethodCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/hardDeletePaymentMethod", async (ISender mediator, int Id) =>
            {
                var command = new HardDeletePaymentMethodCommand(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            
        }
    }
}
