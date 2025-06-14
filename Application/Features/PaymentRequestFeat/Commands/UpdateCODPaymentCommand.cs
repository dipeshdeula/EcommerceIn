using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record UpdateCODPaymentCommand(
        int PaymentRequestId,
        int DeliveryPersonId,
        decimal CollectedAmount,
        string DeliveryStatus, // "Delivered", "PartialPayment", "PaymentRefused"
        string? Notes
    ) : IRequest<Result<PaymentVerificationResponse>>;
    public class UpdateCODPaymentCommandHandler : IRequestHandler<UpdateCODPaymentCommand, Result<PaymentVerificationResponse>>
    {
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly ILogger<UpdateCODPaymentCommandHandler> _logger;

        public UpdateCODPaymentCommandHandler(
            IPaymentGatewayService paymentGatewayService,
            ILogger<UpdateCODPaymentCommandHandler> logger)
        {
            _paymentGatewayService = paymentGatewayService;
            _logger = logger;
        }

        public async Task<Result<PaymentVerificationResponse>> Handle(UpdateCODPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing COD payment update: PaymentRequestId={PaymentRequestId}, DeliveryPerson={DeliveryPersonId}, Status={Status}",
                    request.PaymentRequestId, request.DeliveryPersonId, request.DeliveryStatus);

                var verificationRequest = new PaymentVerificationRequest
                {
                    PaymentRequestId = request.PaymentRequestId,
                    Status = request.DeliveryStatus,
                    DeliveryPersonId = request.DeliveryPersonId,
                    CollectedAmount = request.CollectedAmount,
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
