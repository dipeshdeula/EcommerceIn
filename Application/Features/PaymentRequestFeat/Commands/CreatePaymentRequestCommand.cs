using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record CreatePaymentRequestCommand(
        AddPamentRequestDTO addpaymentRequest
    ) : IRequest<Result<PaymentRequestDTO>>;

    public class CreatePaymentRequestCommandHandler : IRequestHandler<CreatePaymentRequestCommand, Result<PaymentRequestDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGatewayService _paymentGatewayService;

        public CreatePaymentRequestCommandHandler(
            IUnitOfWork unitOfWork,
            IPaymentGatewayService paymentGatewayService)
        {
            _unitOfWork = unitOfWork;
            _paymentGatewayService = paymentGatewayService;
        }

        public async Task<Result<PaymentRequestDTO>> Handle(CreatePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate entities
                var user = await _unitOfWork.Users.GetByIdAsync(request.addpaymentRequest.UserId, cancellationToken);
                var order = await _unitOfWork.Orders.GetByIdAsync(request.addpaymentRequest.OrderId, cancellationToken);
                var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(request.addpaymentRequest.PaymentMethodId, cancellationToken);

                if (user == null)
                {
                    return Result<PaymentRequestDTO>.Failure($"User with ID {request.addpaymentRequest.UserId} not found");
                }

                if (order == null)
                {
                    return Result<PaymentRequestDTO>.Failure($"Order with ID {request.addpaymentRequest.OrderId} not found");
                }

                if (paymentMethod == null)
                {
                    return Result<PaymentRequestDTO>.Failure($"Payment method with ID {request.addpaymentRequest.PaymentMethodId} not found");
                }

                // Validate ownership
                if (user.Id != order.UserId)
                {
                    return Result<PaymentRequestDTO>.Failure($"User ID mismatch: user.Id={user.Id}, order.UserId={order.UserId}");
                }

                // Validate order status
                if (order.PaymentStatus == "Cancelled")
                {
                    return Result<PaymentRequestDTO>.Failure("Cannot create payment for cancelled order");
                }

                // Check for existing pending payment
                var existingPayment = await _unitOfWork.PaymentRequests.GetAsync(
                    predicate: pr => pr.OrderId == order.Id &&
                                   (pr.PaymentStatus == "Pending" || pr.PaymentStatus == "Initiated"),
                    cancellationToken: cancellationToken);

                if (existingPayment != null)
                {
                    return Result<PaymentRequestDTO>.Failure("A pending payment already exists for this order");
                }

                // Create payment request
                var paymentRequest = new PaymentRequest
                {
                    CreatedAt = DateTime.UtcNow,
                    UserId = user.Id,
                    OrderId = order.Id,
                    PaymentMethodId = request.addpaymentRequest.PaymentMethodId,
                    Description = request.addpaymentRequest.Description ?? $"Payment for Order #{order.Id}",
                    PaymentStatus = "Pending",
                    PaymentAmount = order.TotalAmount,
                    Currency = "NPR"
                };

                await _unitOfWork.PaymentRequests.AddAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (paymentRequest.Id == 0)
                {
                    return Result<PaymentRequestDTO>.Failure("Failed to create payment request");
                }

                //  Initiate payment with unified gateway service
                var initiationResult = await _paymentGatewayService.InitiatePaymentAsync(paymentRequest, cancellationToken);

                if (!initiationResult.Succeeded)
                {
                    // Update payment status to failed
                    paymentRequest.PaymentStatus = "Failed";
                    paymentRequest.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<PaymentRequestDTO>.Failure($"Payment initiation failed: {initiationResult.Message}");
                }

                // Build response DTO
                var responseDto = new PaymentRequestDTO
                {
                    Id = paymentRequest.Id,
                    UserId = paymentRequest.UserId,
                    OrderId = paymentRequest.OrderId,
                    PaymentMethodId = paymentRequest.PaymentMethodId,
                    PaymentAmount = paymentRequest.PaymentAmount,
                    Currency = paymentRequest.Currency,
                    Description = paymentRequest.Description,
                    PaymentStatus = paymentRequest.PaymentStatus,
                    CreatedAt = paymentRequest.CreatedAt,
                    UpdatedAt = paymentRequest.UpdatedAt,

                    //  Provider-specific fields
                    PaymentUrl = initiationResult.Data.PaymentUrl,
                    EsewaTransactionId = paymentRequest.EsewaTransactionId,
                    KhaltiPidx = paymentRequest.KhaltiPidx,

                    // Additional metadata
                    ExpiresAt = initiationResult.Data.ExpiresAt,
                    Instructions = initiationResult.Data.Instructions,
                    RequiresRedirect = initiationResult.Data.RequiresRedirect,
                    Metadata = initiationResult.Data.Metadata
                };

                return Result<PaymentRequestDTO>.Success(responseDto, "Payment request created and initiated successfully");
            }
            catch (Exception ex)
            {
                return Result<PaymentRequestDTO>.Failure($"An error occurred while creating payment request: {ex.Message}");
            }
        }
    }
}