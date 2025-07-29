using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.PaymentRequestFeat.DeleteCommands
{
    public record UnDeletePaymentRequestCommand(int Id): IRequest<Result<PaymentRequestDTO>>;

    public class UnDeletePaymentRequestCommandHandler : IRequestHandler<UnDeletePaymentRequestCommand, Result<PaymentRequestDTO>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        public UnDeletePaymentRequestCommandHandler(IPaymentRequestRepository paymentRequestRepository)
        {
            _paymentRequestRepository = paymentRequestRepository;
            
        }
        public async Task<Result<PaymentRequestDTO>> Handle(UnDeletePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            var paymentRequest = await _paymentRequestRepository.FindByIdAsync(request.Id);

            if (paymentRequest == null)
            {
                return Result<PaymentRequestDTO>.Failure("Payment Request Id not found");
            }

            await _paymentRequestRepository.UndeleteAsync(paymentRequest, cancellationToken);
            await _paymentRequestRepository.SaveChangesAsync(cancellationToken);

            return Result<PaymentRequestDTO>.Success(paymentRequest.ToDTO(), "Payment Request unDeleted successfully");
        }
    }
}
