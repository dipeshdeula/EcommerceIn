using Application.Common;
using Application.Features.BillingItemFeat.Commands;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.PaymentRequestFeat.Commands
{
    public record UpdateDeliveryStatusCommand(
        int PaymentRequestId,
        int CompanyId,
        bool IsDelivered
        ) : IRequest<Result<string>>;

    public class UpdateDeliveryStatusCommandHandler : IRequestHandler<UpdateDeliveryStatusCommand, Result<string>>
    {
        private readonly IPaymentRequestRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public UpdateDeliveryStatusCommandHandler(
            IPaymentRequestRepository paymentRepository,
            IOrderRepository orderRepository
            )
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
        }
        public async Task<Result<string>> Handle(UpdateDeliveryStatusCommand request, CancellationToken cancellationToken)
        {
            var checkPayment = await _paymentRepository.FindByIdAsync(request.PaymentRequestId);
            var checkOrder = await _orderRepository.FindByIdAsync(checkPayment.OrderId);

            if (checkPayment == null)
                return Result<string>.Failure("PaymentRequest Id not found");
            if (checkPayment.PaymentStatus != "Succeeded")
                return Result<string>.Failure("Payment is not made yet");
            if (checkOrder.OrderStatus == "COMPLETED")
                return Result<string>.Failure("Order is already delivered");

            if (checkOrder.OrderStatus != "Confirmed")
                return Result<string>.Failure("Order is not confirmed by admin yet");

            checkOrder.OrderStatus = "COMPLETED";
            await _orderRepository.UpdateAsync(checkOrder, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            var companyInfo = await _companyInfoRepository.FindByIdAsync(request.Id);
            if (companyInfo == null)
            {
                return Result<string>.Failure("Company Info not found");
            }


            var generateBill = new CreateBillingItemCommand(checkOrder.UserId, checkOrder.Id, CompanyId);
            if (generateBill == null)
            {
                return Result<string>.Failure("Unable to generate bill");            
            }

            return Result<string>.Success("Order has been delivered");
        }
    }

}
