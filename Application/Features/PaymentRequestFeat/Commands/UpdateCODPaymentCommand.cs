using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record UpdateCODPaymentCommand(
        int PaymentRequestId,
        int DeliveryPersonId,        
        string DeliveryStatus, // "Delivered", "PartialPayment", "PaymentRefused"
        string? Notes
    ) : IRequest<Result<PaymentVerificationResponse>>;
    public class UpdateCODPaymentCommandHandler : IRequestHandler<UpdateCODPaymentCommand, Result<PaymentVerificationResponse>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly ILogger<UpdateCODPaymentCommandHandler> _logger;

        public UpdateCODPaymentCommandHandler(
            IPaymentRequestRepository paymentRequestRepository,
            IPaymentGatewayService paymentGatewayService,
            ILogger<UpdateCODPaymentCommandHandler> logger)
        {
            _paymentRequestRepository = paymentRequestRepository;
            _paymentGatewayService = paymentGatewayService;
            _logger = logger;
        }

        public async Task<Result<PaymentVerificationResponse>> Handle(UpdateCODPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing COD payment update: PaymentRequestId={PaymentRequestId}, DeliveryPerson={DeliveryPersonId}, Status={Status}",
                    request.PaymentRequestId, request.DeliveryPersonId, request.DeliveryStatus);

                var payment = await _paymentRequestRepository.FindByIdAsync(request.PaymentRequestId);
                if (payment == null)
                    return Result<PaymentVerificationResponse>.Failure("PaymentRequest id not found");
                    
               

                var verificationRequest = new PaymentVerificationRequest
                {
                    PaymentRequestId = request.PaymentRequestId,
                    PaymentStatus = request.DeliveryStatus,
                    DeliveryPersonId = request.DeliveryPersonId,
                    CollectedAmount = payment.PaymentAmount,
                    DeliveryNotes = request.Notes
                };

                var result = await _paymentGatewayService.VerifyPaymentAsync(verificationRequest, cancellationToken);

                if (result.Succeeded)
                {
                    _logger.LogInformation(" COD payment updated successfully: PaymentRequestId={PaymentRequestId}",
                        request.PaymentRequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to update COD payment: PaymentRequestId={PaymentRequestId}",
                    request.PaymentRequestId);
                return Result<PaymentVerificationResponse>.Failure($"Failed to update COD payment: {ex.Message}");
            }
        }
    }
}
