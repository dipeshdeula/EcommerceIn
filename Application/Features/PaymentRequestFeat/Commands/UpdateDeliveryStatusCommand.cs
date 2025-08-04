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
        private readonly IMediator _mediator;
        public UpdateDeliveryStatusCommandHandler(
            IPaymentRequestRepository paymentRepository,
            IOrderRepository orderRepository,
            ICompanyInfoRepository companyInfoRespository,
            IMediator mediator
            )
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _companyInfoRepository = companyInfoRespository;
            _mediator = mediator;
        }
        public async Task<Result<string>> Handle(UpdateDeliveryStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var checkPayment = await _paymentRepository.FindByIdAsync(request.PaymentRequestId);

                // validate payment request
                if (checkPayment == null)
                    return Result<string>.Failure("PaymentRequest Id not found");

                // validate payment status

                if (checkPayment.PaymentStatus != "Succeeded" && checkPayment.PaymentStatus != "Paid")
                    return Result<string>.Failure("Payment is not completed yet");

                var checkOrder = await _orderRepository.FindByIdAsync(checkPayment.OrderId);
                if(checkOrder == null)                
                    return Result<string>.Failure("Order Id not found");

                if (checkOrder.OrderStatus == "COMPLETED")
                        return Result<string>.Failure("Order is already delivered");

                if (checkOrder.OrderStatus != "Confirmed")
                    return Result<string>.Failure("Order is not confirmed by admin yet");

                checkOrder.OrderStatus = "COMPLETED";

                // validate company info
                var companyInfo = await _companyInfoRepository.FindByIdAsync(request.CompanyId);
                if (companyInfo == null)
                {
                    return Result<string>.Failure("Company Info not found");
                }

                // update order status to COMPLETED
                checkOrder.OrderStatus = "COMPLETED";
                await _orderRepository.UpdateAsync(checkOrder, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);

               
                // Generating billing items dynamically
                var generateBill = new CreateBillingItemCommand(checkOrder.UserId, checkOrder.Id, request.CompanyId);
                var billingResult = await _mediator.Send(generateBill, cancellationToken);
                if (!billingResult.Succeeded)
                {
                    
                    // Since order is already marked as delivered
                    return Result<string>.Success(
                        $"Order has been delivered successfully, but billing generation failed: {billingResult.Message}");
                }

                return Result<string>.Success(
                    $"Order has been delivered and billing items created successfully. " +
                    $"Generated {billingResult.Data?.Count ?? 0} billing items.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"An error occurred while updating delivery status: {ex.Message}");
            }

        }
    }

}
