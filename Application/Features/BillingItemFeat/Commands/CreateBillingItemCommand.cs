using Application.Common;
using Application.Dto.BilItemDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BillingItemFeat.Commands
{
    public record CreateBillingItemCommand(
        int UserId,
        int OrderId,
        int CompanyId
        
    ) : IRequest<Result<List<BillingItemDTO>>>;

    public class CreateBillingItemCommandHandler : IRequestHandler<CreateBillingItemCommand, Result<List<BillingItemDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        public CreateBillingItemCommandHandler(
            IUnitOfWork unitOfWork,
            IOrderItemRepository orderItemRepository,
            IPaymentMethodRepository paymentMethodRepository
            )
        {
            _unitOfWork = unitOfWork;
            _orderItemRepository = orderItemRepository;
            _paymentMethodRepository = paymentMethodRepository;
        }

        public async Task<Result<List<BillingItemDTO>>> Handle(CreateBillingItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Fetch user
                var user = await _unitOfWork.Users.FindByIdAsync(request.UserId);

                // Fetch order with items
                var order = await _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product) // If you want product info
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted, cancellationToken);

                var company = await _unitOfWork.CompanyInfos.FindByIdAsync(request.CompanyId);

                var payment = await _unitOfWork.PaymentRequests.FirstOrDefaultAsync(p => p.OrderId == order.Id);

                if (user == null)
                    return Result<List<BillingItemDTO>>.Failure("User id not found");

                if (order == null)
                    return Result<List<BillingItemDTO>>.Failure("Order id not found");

                if (company == null)
                    return Result<List<BillingItemDTO>>.Failure("Company Id not found");
                if (payment == null)
                    return Result<List<BillingItemDTO>>.Failure("payment method Id not found");

                if (user.Id != order.UserId)
                    return Result<List<BillingItemDTO>>.Failure("UserId is not associated with OrderId");

                // Check Payment status
                if (order.PaymentStatus != "Confirmed" || order.PaymentStatus != "Paid")
                    return Result<List<BillingItemDTO>>.Failure("Cannot generate billing items for incomplete Payment.");
                // Check Order status
                if (order.OrderStatus != "COMPLETED")
                    return Result<List<BillingItemDTO>>.Failure("Cannot generate billing items for incomplete Order.");
                

                // Use order.Items directly
                var orderItems = order.Items?.Where(i => !i.IsDeleted).ToList();
                if (orderItems == null || !orderItems.Any())
                    return Result<List<BillingItemDTO>>.Failure("No order items found for this order.");

                var billing = new Billing
                {
                    UserId = user.Id,
                    OrderId = order.Id,
                    CompanyInfoId = request.CompanyId,
                    PaymentId = payment.Id,
                    BillingDate = DateTime.UtcNow,
                    User = user,
                    Order = order,
                    CompanyInfo = company
                };

                await _unitOfWork.Billings.AddAsync(billing, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create billing items (snapshot)
                var billingItems = orderItems.Select(item => new BillingItem
                {
                    BillingId = billing.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? string.Empty,
                    ProductSku = item.Product?.Sku ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.UnitPrice * item.Quantity,
                    DiscountAmount = null,
                    TaxAmount = null,
                    Notes = null
                }).ToList();

                await _unitOfWork.BillingItems.AddRangeAsync(billingItems, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Map to DTOs
                var billingItemDTOs = billingItems.Select(bi => new BillingItemDTO
                {
                    Id = bi.Id,
                    ProductId = bi.ProductId,
                    ProductName = bi.ProductName,
                    ProductSku = bi.ProductSku,
                    Quantity = bi.Quantity,
                    UnitPrice = bi.UnitPrice,
                    TotalPrice = bi.TotalPrice,
                    DiscountAmount = bi.DiscountAmount,
                    TaxAmount = bi.TaxAmount,
                    Notes = bi.Notes
                }).ToList();

                return Result<List<BillingItemDTO>>.Success(billingItemDTOs, "Billing items created successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<BillingItemDTO>>.Failure("Unable to generate bill");
            }
        }
    }
}