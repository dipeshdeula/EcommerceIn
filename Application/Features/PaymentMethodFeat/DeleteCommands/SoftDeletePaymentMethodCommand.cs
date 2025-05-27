using Application.Common;
using Application.Dto;
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
    public record SoftDeletePaymentMethodCommand (int Id) : IRequest<Result<PaymentMethodDTO>>;

    public class SoftDeletePaymentMethodCommandHandler : IRequestHandler<SoftDeletePaymentMethodCommand, Result<PaymentMethodDTO>>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        public SoftDeletePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepsitory)
        {
            _paymentMethodRepository = paymentMethodRepsitory;
            
        }
        public async Task<Result<PaymentMethodDTO>> Handle(SoftDeletePaymentMethodCommand request, CancellationToken cancellationToken)
        {
           var payment = await _paymentMethodRepository.FindByIdAsync(request.Id);
            if (payment == null)
                return Result<PaymentMethodDTO>.Failure("Payment Id is not found");

            await _paymentMethodRepository.SoftDeleteAsync(payment,cancellationToken);
            await _paymentMethodRepository.SaveChangesAsync(cancellationToken);

            return Result<PaymentMethodDTO>.Success(payment.ToDTO(), "Payment method soft deleted successfully");

        }
    }
}
