using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.PaymentRequestFeat.DeleteCommands
{
    public record HardDeletePaymentRequestCommand (int Id) : IRequest<Result<PaymentRequestDTO>>;

    public class HardDeletePaymentRequestCommandHandler : IRequestHandler<HardDeletePaymentRequestCommand, Result<PaymentRequestDTO>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        public HardDeletePaymentRequestCommandHandler(IPaymentRequestRepository paymentRequestRepository)
        {
            _paymentRequestRepository = paymentRequestRepository;
        }
        public async Task<Result<PaymentRequestDTO>> Handle(HardDeletePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            var paymentRequests = await _paymentRequestRepository.FindByIdAsync(request.Id);
            if (paymentRequests == null)
            {
                return Result<PaymentRequestDTO>.Failure("Payment Request Id not found");
            }

            await _paymentRequestRepository.RemoveAsync(paymentRequests, cancellationToken);
            await _paymentRequestRepository.SaveChangesAsync(cancellationToken);

            return Result<PaymentRequestDTO>.Success(paymentRequests.ToDTO(), "Payment Request hard deleted successfully");


        }
    }

}
