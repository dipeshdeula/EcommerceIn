using Application.Dto;
using Application.Dto.PaymentDTOs;
using Application.Features.PaymentRequestFeat.Commands;
using Application.Features.PaymentRequestFeat.Queries;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PaymentRequestFeat.Module
{
    public class PaymentRequestMod : CarterModule
    {
        public PaymentRequestMod() : base("")
        {
            WithTags("Payment");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("Payment");

            app.MapPost("/create-payment-intent", async (
                AddPamentRequestDTO addPaymentRequest,
                ISender mediator

                ) =>
            {
                var command = new CreatePaymentRequestCommand(addPaymentRequest);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapGet("getAllPaymentRequests", async (
                ISender mediator, int PageNumber = 1, int PageSize = 10
                ) =>
            {
                var command = new GetAllPaymentQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("getPaymentByUserId", async (
                 ISender mediator, int UserId, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetPaymentByUserIdQuery(UserId, PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/verifyPayment", async (
                int PaymentRequestId,               
                string? EsewaTransactionId,
                string? KhaltiPidx, string? Status,ISender mediator
                ) =>
            {
                var command = new VerifyPaymentCommand(PaymentRequestId,EsewaTransactionId,KhaltiPidx,Status);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                { 
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });
        }
    }
}
