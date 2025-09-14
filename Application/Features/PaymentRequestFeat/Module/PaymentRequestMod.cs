using Application.Dto.PaymentDTOs;
using Application.Features.PaymentRequestFeat.Commands;
using Application.Features.PaymentRequestFeat.DeleteCommands;
using Application.Features.PaymentRequestFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PaymentRequestFeat.Module
{
    public class PaymentRequestModule : CarterModule
    {
        public PaymentRequestModule() : base("/payment")
        {
            WithTags("Payment");
            IncludeInOpenApi();
            //RequireAuthorization();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-payment-intent", async (
                AddPamentRequestDTO addPaymentRequest,
                ISender mediator) =>
            {
                var command = new CreatePaymentRequestCommand(addPaymentRequest);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        result.Message,
                        result.Errors,
                        success = false,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Results.Ok(new
                {
                    result.Message,
                    result.Data,
                    //success = true,
                    //timestamp = DateTime.UtcNow
                });
            })
            .WithName("CreatePaymentIntent")
            .WithSummary("Create payment intent for order")
            .WithDescription("Initiates payment process for the specified order")
            .Produces<PaymentInitiationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

           

            // Get all payment requests

            app.MapGet("/requests", async (
                ISender mediator,
                int pageNumber = 1,
                int pageSize = 10,
                string? status = null,
                int? PaymentMethodId = null,
                DateTime? FromDate = null,
                DateTime? ToDate = null,
                string? SearchTerm = null,
                string? OrderBy = "CreatedAt"


                ) =>
            {
                var query = new GetAllPaymentQuery(
                    pageNumber, pageSize, status, PaymentMethodId, FromDate, ToDate, SearchTerm, OrderBy);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { 
                    result.Message, result.Data, result.TotalCount,result.TotalPages,result.PageSize,result.HasNextPage,result.HasPreviousPage });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("GetAllPaymentRequests")
            .WithSummary("Get all payment requests")
            .WithDescription("Retrieve paginated list of payment requests")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // get payment by userId
            app.MapGet("/user/{userId}", async (
                int userId,
                ISender mediator,
                int pageNumber = 1,
                int pageSize = 10,
                string? Status = null,
                string? OrderBy = "CreatedAt"
                ) =>
            {
                var query = new GetPaymentByUserIdQuery(userId, pageNumber, pageSize, Status,OrderBy);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("GetPaymentsByUserId")
            .WithSummary("Get payments by user ID")
            .WithDescription("Retrieve paginated list of payments for specific user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // soft delete
            app.MapDelete("/softDeletPaymentRequest", async (int Id, ISender mediator) =>
            {
                var command = new SoftDeletePaymentRequestCommand(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });

            // unDelete Payment Request
            app.MapDelete("/unDeletePaymentRequest", async (int Id, ISender mediator) =>
            {
                var command = new UnDeletePaymentRequestCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });

                }

                return Results.Ok(new { result.Message, result.Data });
            });

            // Hard Delete Payment Request
            app.MapDelete("/hardDeletePaymentRequest", async (int Id, ISender mediator) =>
            {
                var command = new HardDeletePaymentRequestCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Data });
                }
                return Results.Ok(new { result.Message, result.Data });
            });
        }
    }
}