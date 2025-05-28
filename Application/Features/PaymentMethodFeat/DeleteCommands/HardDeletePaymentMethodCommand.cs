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
    public record HardDeletePaymentMethodCommand (int Id) : IRequest<Result<PaymentMethodDTO>>;

    public class HardDeletePaymentMethodCommandHandler : IRequestHandler<HardDeletePaymentMethodCommand, Result<PaymentMethodDTO>>
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        public HardDeletePaymentMethodCommandHandler(IPaymentMethodRepository paymentMethodRepository)
        {
            _paymentMethodRepository = paymentMethodRepository;
        }
        public async Task<Result<PaymentMethodDTO>> Handle(HardDeletePaymentMethodCommand request, CancellationToken cancellationToken)
        {
           var payment = await _paymentMethodRepository.FindByIdAsync(request.Id);
            if (payment == null)
                return Result<PaymentMethodDTO>.Failure("Payment Id is not found");
            await _paymentMethodRepository.RemoveAsync(payment, cancellationToken);
            await _paymentMethodRepository.SaveChangesAsync(cancellationToken);

            return Result<PaymentMethodDTO>.Success(payment.ToDTO(), "Payment Id is Hard deleted successfully");

        }
    }

}
