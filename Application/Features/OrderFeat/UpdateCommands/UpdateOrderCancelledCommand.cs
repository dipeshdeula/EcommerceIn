using Application.Common;
using Application.Dto.OrderDTOs;
using Application.Dto.Shared;
using Application.Features.OrderFeat.Events;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.UpdateCommands
{
    public record UpdateOrderCancelledCommand
    (
        int OrderId,
        string ReasonToCancel,
        bool IsCancelled
    ) : IRequest<Result<OrderCancellationRepsonseDTO>>;

    public class UpdateOrderCancelledCommandHandler : IRequestHandler<UpdateOrderCancelledCommand, Result<OrderCancellationRepsonseDTO>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _productPricingService;
        private readonly IEventUsageService _eventUsageService;
        private readonly ILogger<UpdateOrderCancelledCommandHandler> _logger;
        private readonly IMediator _mediator;

        public UpdateOrderCancelledCommandHandler(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUserserice,
            IEmailService emailService,
            IRabbitMqPublisher rabbitMqPublisher,
            IProductRepository productRepository,
            IProductPricingService productPricingService,
            IEventUsageService eventUsageService,
            ILogger<UpdateOrderCancelledCommandHandler> logger,
            IMediator mediator
            )
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _currentUserService = currentUserserice;
            _emailService = emailService;
            _productPricingService = productPricingService;
            _eventUsageService = eventUsageService;
            _productRepository = productRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logger = logger;
            _mediator = mediator;

        }
        public async Task<Result<OrderCancellationRepsonseDTO>> Handle(UpdateOrderCancelledCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(" Processing order confirmation: OrderId={OrderId}, IsCancelled={IsCancelled}",
                    request.OrderId, request.IsCancelled);

                var order = await _orderRepository.GetAsync(
                    predicate: o => o.Id == request.OrderId && !o.IsDeleted,
                    includeProperties: "Items,User",
                    cancellationToken: cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: OrderId={OrderId}", request.OrderId);
                    return Result<OrderCancellationRepsonseDTO>.Failure("Order not found or has been deleted.");
                }


                var authResult = ValidateUserAuthorization(order.UserId);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning(" Authorization failed for order cancellation: {Error}", authResult.Message);
                    return Result<OrderCancellationRepsonseDTO>.Failure(authResult.Message);
                }


                if (order.IsCancelled && request.IsCancelled && !order.IsDeleted)
                {
                    _logger.LogWarning("Order already cancelled: OrderId={OrderId}", request.OrderId);
                    return Result<OrderCancellationRepsonseDTO>.Failure("Order is already cancelled or not available.");
                }


                var user = order.User ?? await _userRepository.GetAsync(
                    predicate: u => u.Id == order.UserId && !u.IsDeleted,
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    _logger.LogError("User not found for order: OrderId={OrderId}, UserId={UserId}",
                        request.OrderId, order.UserId);
                    return Result<OrderCancellationRepsonseDTO>.Failure("User not found for this order.");
                }


                var previousStatus = order.OrderStatus;
                var previousCancelled = order.IsCancelled;

                order.OrderStatus = request.IsCancelled ? "Cancelled" : "Pending";
                order.IsCancelled = request.IsCancelled;
                order.ReasonToCancel = request.IsCancelled ? request.ReasonToCancel : null;
                order.IsConfirmed = false;
                order.UpdatedAt = DateTime.UtcNow;

                await _orderRepository.UpdateAsync(order, cancellationToken);

                _logger.LogInformation("Order status updated: OrderId={OrderId}, Status={Status}, IsCancelled={IsCancelled}",
                    order.Id, order.PaymentStatus, order.IsCancelled);

                // restore prodcut stock
                await RestoreProductStock(order);

                // reverse event usage
                var eventUsageResult = await _eventUsageService.ReverseEventUsageForOrderAsync(order.Id, order.UserId);
                _logger.LogInformation("Event usage reversed: OrderId={OrderId}, Result={Result}",
                    order.Id, eventUsageResult);

                // Invalidate pricing cache
                await InvalidatePricingCacheForOrder(order);

                await _orderRepository.SaveChangesAsync(cancellationToken);



                var notificationResult = new NotificationResultDTO { EmailSent = false, RabbitMqSent = false };

                if (request.IsCancelled)
                {
                    notificationResult = await SendOrderCancellationNotificationsAsync(order, user, cancellationToken);
                }

                // publish events
                await _mediator.Publish(new OrderCancelledEvents
                {
                    Order = order,
                    User = user,

                }, cancellationToken);


                var responseDto = new OrderCancellationRepsonseDTO
                {
                    OrderId = order.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = order.PaymentStatus,
                    OrderStatus = order.OrderStatus,
                    PreviousCancelled = previousCancelled,
                    IsCancelled = order.IsCancelled,
                    ReasonToCancel = order.ReasonToCancel ?? "",
                    EventUsageResult = eventUsageResult.Succeeded ? eventUsageResult.Data : eventUsageResult.Message,
                    UpdatedAt = order.UpdatedAt.Value,
                    UserEmail = user.Email,
                    UserName = user.Name,
                    TotalAmount = order.TotalAmount,
                    NotificationResult = notificationResult,
                    Message = "Order cancelled successfully"
                };

                var successMessage = request.IsCancelled
                    ? $"Order #{order.Id} cancelled successfully. Customer will be notified."
                    : $"Order #{order.Id} status updated to pending.";

                _logger.LogInformation("Order cancellation completed: OrderId={OrderId}, EmailSent={EmailSent}, RabbitMqSent={RabbitMqSent}",
                    order.Id, notificationResult.EmailSent, notificationResult.RabbitMqSent);

                return Result<OrderCancellationRepsonseDTO>.Success(responseDto, successMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order cancellatin: OrderId={OrderId}", request.OrderId);
                return Result<OrderCancellationRepsonseDTO>.Failure($"Failed to update order: {ex.Message}");
            }
        }

        /// <summary>
        ///  VALIDATE USER AUTHORIZATION with proper role checking
        /// </summary>
        private Result<bool> ValidateUserAuthorization(int userId)
        {
            
            if (_currentUserService == null || !_currentUserService.IsAuthenticated)
            {
                return Result<bool>.Failure("User not authenticated.");
            }

            if (_currentUserService.UserId != userId.ToString())
            {
                return Result<bool>.Failure("User id is not matched");
            }
                       

           

            return Result<bool>.Success(true);
        }

        private async Task RestoreProductStock(Order order)
        { 
            try
            {
                if (order.Items?.Any() == true)
                {
                    foreach (var item in order.Items)
                    {
                        var product = await _productRepository.GetByIdAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity;
                            await _productRepository.UpdateAsync(product);
                            
                            _logger.LogDebug("Restored stock for product {ProductId}: +{Quantity}", 
                                item.ProductId, item.Quantity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring product stock for order {OrderId}", order.Id);
            }

        }

        private async Task InvalidatePricingCacheForOrder(Order order)
        {
            try
            {
                if (order.Items?.Any() == true)
                {
                    foreach (var item in order.Items)
                    {
                        await _productPricingService.InvalidatePriceCacheAsync(item.ProductId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating pricing cache for order {OrderId}", order.Id);
            }
        }



        /// <summary>
        ///  SEND COMPREHENSIVE NOTIFICATIONS 
        /// </summary>
        private async Task<NotificationResultDTO> SendOrderCancellationNotificationsAsync(
            Domain.Entities.Order order, Domain.Entities.User user, CancellationToken cancellationToken)
        {
            var result = new NotificationResultDTO();


            try
            {
                //  1. SEND EMAIL NOTIFICATION
                await SendOrderCancellationEmailAsync(order, user);
                result.EmailSent = true;
                result.EmailError = null;

                _logger.LogInformation(" Email notification sent: OrderId={OrderId}, UserEmail={Email}",
                    order.Id, user.Email);
            }
            catch (Exception emailEx)
            {
                result.EmailSent = false;
                result.EmailError = emailEx.Message;
                _logger.LogError(emailEx, " Failed to send email notification: OrderId={OrderId}, UserEmail={Email}",
                    order.Id, user.Email);
            }

            try
            {
                await _mediator.Publish(new OrderCancelledEvents
                {
                    Order = order,
                    User = user,

                });
                //_rabbitMqPublisher.Publish("OrderConfirmedQueue", orderCancelledEvent, Guid.NewGuid().ToString(), null);
                //result.RabbitMqSent = true;
                //result.RabbitMqError = null;

                _logger.LogInformation(" RabbitMQ notification sent: OrderId={OrderId}", order.Id);
            }
            catch (Exception rabbitEx)
            {
                result.RabbitMqSent = false;
                result.RabbitMqError = rabbitEx.Message;
                _logger.LogError(rabbitEx, " Failed to send RabbitMQ notification: OrderId={OrderId}", order.Id);
            }

            return result;
        }

        /// <summary>
        ///  SEND DETAILED EMAIL Cancellation
        /// </summary>
        private async Task SendOrderCancellationEmailAsync(Domain.Entities.Order order, Domain.Entities.User user)
        {
            var subject = $"Order Cancellation - Order #{order.Id}";

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                        <h1 style='margin: 0; font-size: 28px;'> Order Cancelled!</h1>
                    </div>
                    
                    <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #28a745;'>
                            <h2 style='color: #28a745; margin-top: 0;'>Hello {user.Name}! 👋</h2>
                            
                            <p style='font-size: 16px; line-height: 1.6; color: #333;'>
                                 Your order has been <strong>cancelled</strong> and Thank you for visiting us.
                            </p>                          

                            
                           

                            <div style='text-align: center; margin: 30px 0;'>
                                <p style='color: #666; font-size: 14px; margin: 0;'>
                                    Questions about your order? Contact us at<br>
                                    📧 support@getinstantmart.com | 📞 +977-XXX-XXXX
                                </p>
                            </div>

                            <div style='border-top: 2px solid #eee; padding-top: 20px; text-align: center;'>
                                <p style='color: #28a745; font-size: 18px; font-weight: bold; margin: 0;'>
                                    Thank you for choosing GetInstantMart! 
                                </p>
                                <p style='color: #666; font-size: 14px; margin: 5px 0 0 0;'>
                                    Fast delivery, great service, every time.
                                </p>
                            </div>
                        </div>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(user.Email, subject, emailBody);
        }
    }
}

