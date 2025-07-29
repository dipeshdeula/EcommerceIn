using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.PaymentRequestFeat.DeleteCommands
{
    public record SoftDeletePaymentRequestCommand(int Id): IRequest<Result<PaymentRequestDTO>>;

    public class SoftDeletePaymentRequestCommandHandler : IRequestHandler<SoftDeletePaymentRequestCommand, Result<PaymentRequestDTO>>
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        public SoftDeletePaymentRequestCommandHandler(IPaymentRequestRepository paymentRequestRepository)
        {
            _paymentRequestRepository = paymentRequestRepository;
        }
        
        public async Task<Result<PaymentRequestDTO>> Handle(SoftDeletePaymentRequestCommand request, CancellationToken cancellationToken)
        {
            var paymentRequests = await _paymentRequestRepository.FindByIdAsync(request.Id);

            if (paymentRequests == null)
            {
                return Result<PaymentRequestDTO>.Failure("Payment Request Id not found");
            }

            await _paymentRequestRepository.SoftDeleteAsync(paymentRequests, cancellationToken);

            return Result<PaymentRequestDTO>.Success(paymentRequests.ToDTO(), "Payment Request soft deleted successfully");

        }
    }

}
