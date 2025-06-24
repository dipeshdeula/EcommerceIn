using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record VerifyPaymentCommand(
        int PaymentRequestId,              
        string? EsewaTransactionId,
        string? KhaltiPidx,
        string? Status
        ) : IRequest<Result<bool>>;

    public class VerifyPaymentCommandHandler : IRequestHandler<VerifyPaymentCommand, Result<bool>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        private readonly IOrderRepository _orderRepository;
        public VerifyPaymentCommandHandler(IPaymentRequestRepository paymentMethodRepository, IOrderRepository orderRepository)
        {
            _paymentRequestRepository = paymentMethodRepository;
            _orderRepository = orderRepository;
        }

        public async Task<Result<bool>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
        {
            var paymentRequest = await _paymentRequestRepository.FindByIdAsync(request.PaymentRequestId);
            if (paymentRequest == null)
            {
                return Result<bool>.Failure("PaymentRequestId not found");

            }
            if (paymentRequest.PaymentMethodId.Equals(1))
            {
                // Update status and transaction ID from frontend
                paymentRequest.PaymentStatus = request.Status == "SUCCESS" ? "Succeeded" : "Failed";
                paymentRequest.EsewaTransactionId = request.EsewaTransactionId;
                paymentRequest.UpdatedAt = DateTime.UtcNow;
                await _paymentRequestRepository.UpdateAsync(paymentRequest, cancellationToken);
                if (paymentRequest.PaymentStatus == "Succeeded")
                {

                    var order = new Order
                    {
                        
                        PaymentStatus = paymentRequest.PaymentStatus
                    };

                    await _orderRepository.UpdateAsync(order, cancellationToken);
                        
                }
                return Result<bool>.Success(request.Status == "SUCCESS", "Payment Successful");
            }
            else if (paymentRequest.PaymentMethodId.Equals(2))
            {
                // Similar logic for Khalti if handled on frontend
                paymentRequest.PaymentStatus = request.Status == "Completed" ? "Succeeded" : "Failed";
                paymentRequest.KhaltiPidx = request.KhaltiPidx;
                paymentRequest.UpdatedAt = DateTime.UtcNow;
                await _paymentRequestRepository.UpdateAsync(paymentRequest, cancellationToken);

                if (paymentRequest.PaymentStatus == "Succeeded")
                {
                    var order = new Order
                    {
                        PaymentStatus = paymentRequest.PaymentStatus
                    };

                    await _orderRepository.UpdateAsync(order, cancellationToken);

                }
                return Result<bool>.Success(request.Status == "Completed", "Payment Successful");
            }

            else if (paymentRequest.PaymentMethodId.Equals(3))
            {
                paymentRequest.PaymentStatus = request.Status == "Completed" ? "Succeeded" : "Failed";
                paymentRequest.UpdatedAt= DateTime.UtcNow;
                await _paymentRequestRepository.UpdateAsync(paymentRequest,cancellationToken);

                if (paymentRequest.PaymentStatus == "Succeeded")
                {
                    var order = new Order
                    {
                        PaymentStatus = paymentRequest.PaymentStatus
                    };

                    await _orderRepository.UpdateAsync(order, cancellationToken);

                }
                return Result<bool>.Success(request.Status == "completed", "Payment Successful");
            }

            return Result<bool>.Failure("Transaction Failed !!");

        }
    }
}
