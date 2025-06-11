using Application.Common;
using Application.Dto.PaymentMethodDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.PaymentMethodFeat.DeleteCommands
{
    public record UnDeletePaymentMethodCommand(int Id) : IRequest<Result<PaymentMethodDTO>>;

    public class UnDeletePaymentMethodCommandHandler : IRequestHandler<UnDeletePaymentMethodCommand, Result<PaymentMethodDTO>>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        public UnDeletePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository)
        {
            _paymentMethodRepository = paymentMethodRepository;
            
        }
        public async Task<Result<PaymentMethodDTO>> Handle(UnDeletePaymentMethodCommand request, CancellationToken cancellationToken)
        {
            var payment = await _paymentMethodRepository.FindByIdAsync(request.Id);
            if (payment == null)
                return Result<PaymentMethodDTO>.Failure("Payment id is not found");

            await _paymentMethodRepository.UndeleteAsync(payment,cancellationToken);
            await _paymentMethodRepository.SaveChangesAsync(cancellationToken);

            return Result<PaymentMethodDTO>.Success(payment.ToDTO(), "Payment method undeleted successfully");
        }
    }
}
