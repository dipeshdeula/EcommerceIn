using Application.Common;
using Application.Dto.OrderDTOs;
using Application.Dto.Shared;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Features.OrderFeat.UpdateCommands
{
    public record UpdateOrderConfirmedCommand(
        int OrderId,
        bool IsConfirmed
    ) : IRequest<Result<OrderConfirmationResponseDTO>>;

    public class UpdateOrderConfirmedCommandHandler : IRequestHandler<UpdateOrderConfirmedCommand, Result<OrderConfirmationResponseDTO>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly ILogger<UpdateOrderConfirmedCommandHandler> _logger;

        public UpdateOrderConfirmedCommandHandler(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IRabbitMqPublisher rabbitMqPublisher,
            ILogger<UpdateOrderConfirmedCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logger = logger;
        }

        public async Task<Result<OrderConfirmationResponseDTO>> Handle(UpdateOrderConfirmedCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(" Processing order confirmation: OrderId={OrderId}, IsConfirmed={IsConfirmed}",
                    request.OrderId, request.IsConfirmed);

                var order = await _orderRepository.GetAsync(
                    predicate: o => o.Id == request.OrderId && !o.IsDeleted,
                    includeProperties: "Items,User",
                    cancellationToken: cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: OrderId={OrderId}", request.OrderId);
                    return Result<OrderConfirmationResponseDTO>.Failure("Order not found or has been deleted.");
                }

               
                var authResult = ValidateUserAuthorization();
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning(" Authorization failed for order confirmation: {Error}", authResult.Message);
                    return Result<OrderConfirmationResponseDTO>.Failure(authResult.Message);
                }

  
                if (order.IsConfirmed && request.IsConfirmed)
                {
                    _logger.LogWarning("Order already confirmed: OrderId={OrderId}", request.OrderId);
                    return Result<OrderConfirmationResponseDTO>.Failure("Order is already confirmed.");
                }


                var user = order.User ?? await _userRepository.GetAsync(
                    predicate: u => u.Id == order.UserId && !u.IsDeleted,
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    _logger.LogError("❌ User not found for order: OrderId={OrderId}, UserId={UserId}",
                        request.OrderId, order.UserId);
                    return Result<OrderConfirmationResponseDTO>.Failure("User not found for this order.");
                }

              
                var previousStatus = order.Status;
                var previousConfirmed = order.IsConfirmed;

                order.Status = request.IsConfirmed ? "Confirmed" : "Pending";
                order.IsConfirmed = request.IsConfirmed;
                order.UpdatedAt = DateTime.UtcNow;

                await _orderRepository.UpdateAsync(order, cancellationToken);

               
                await _orderRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✅ Order status updated: OrderId={OrderId}, Status={Status}, IsConfirmed={IsConfirmed}",
                    order.Id, order.Status, order.IsConfirmed);

                
                var notificationResult = new NotificationResultDTO { EmailSent = false, RabbitMqSent = false };

                if (request.IsConfirmed)
                {
                    notificationResult = await SendOrderConfirmationNotificationsAsync(order, user, cancellationToken);
                }

               
                var responseDto = new OrderConfirmationResponseDTO
                {
                    OrderId = order.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = order.Status,
                    PreviousConfirmed = previousConfirmed,
                    IsConfirmed = order.IsConfirmed,
                    UpdatedAt = order.UpdatedAt.Value,
                    UserEmail = user.Email,
                    UserName = user.Name,
                    TotalAmount = order.TotalAmount,
                    ShippingAddress = order.ShippingAddress,
                    EstimatedDeliveryMinutes = request.IsConfirmed ? 25 : null, // ETA only when confirmed
                    NotificationResult = notificationResult
                };

                var successMessage = request.IsConfirmed
                    ? $"Order #{order.Id} confirmed successfully. Customer will be notified."
                    : $"Order #{order.Id} status updated to pending.";

                _logger.LogInformation("🎉 Order confirmation completed: OrderId={OrderId}, EmailSent={EmailSent}, RabbitMqSent={RabbitMqSent}",
                    order.Id, notificationResult.EmailSent, notificationResult.RabbitMqSent);

                return Result<OrderConfirmationResponseDTO>.Success(responseDto, successMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update order confirmation: OrderId={OrderId}", request.OrderId);
                return Result<OrderConfirmationResponseDTO>.Failure($"Failed to update order: {ex.Message}");
            }
        }

        /// <summary>
        ///  VALIDATE USER AUTHORIZATION with proper role checking
        /// </summary>
        private Result<bool> ValidateUserAuthorization()
        {
            var userClaims = _httpContextAccessor.HttpContext?.User;
            if (userClaims == null || !userClaims.Identity.IsAuthenticated)
            {
                return Result<bool>.Failure("User not authenticated.");
            }

            var role = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
            {
                return Result<bool>.Failure("User role not found.");
            }

            // ✅ Allow Admin and SuperAdmin to confirm orders
            if (role != UserRoles.Admin.ToString() && role != UserRoles.SuperAdmin.ToString())
            {
                return Result<bool>.Failure("Only Admin or SuperAdmin can confirm orders.");
            }

            return Result<bool>.Success(true);
        }

        /// <summary>
        ///  SEND COMPREHENSIVE NOTIFICATIONS 
        /// </summary>
        private async Task<NotificationResultDTO> SendOrderConfirmationNotificationsAsync(
            Domain.Entities.Order order, Domain.Entities.User user, CancellationToken cancellationToken)
        {
            var result = new NotificationResultDTO();
            const int etaMinutes = 25; // Business logic: 25 minutes ETA

            try
            {
                //  1. SEND EMAIL NOTIFICATION
                await SendOrderConfirmationEmailAsync(order, user, etaMinutes);
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
                // ✅ 2. PUBLISH TO RABBITMQ
                var orderConfirmedEvent = new OrderConfirmedEventDTO
                {
                    OrderId = order.Id,
                    UserId = user.Id,
                    UserEmail = user.Email,
                    UserName = user.Name,
                    EtaMinutes = etaMinutes
                };

                _rabbitMqPublisher.Publish("OrderConfirmedQueue", orderConfirmedEvent, Guid.NewGuid().ToString(), null);
                result.RabbitMqSent = true;
                result.RabbitMqError = null;

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
        ///  SEND DETAILED EMAIL CONFIRMATION
        /// </summary>
        private async Task SendOrderConfirmationEmailAsync(Domain.Entities.Order order, Domain.Entities.User user, int etaMinutes)
        {
            var subject = $"Order Confirmation - Order #{order.Id}";
            var deliveryTime = DateTime.Now.AddMinutes(etaMinutes);

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                        <h1 style='margin: 0; font-size: 28px;'>🎉 Order Confirmed!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 18px; opacity: 0.9;'>Your order is on its way</p>
                    </div>
                    
                    <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #28a745;'>
                            <h2 style='color: #28a745; margin-top: 0;'>Hello {user.Name}! 👋</h2>
                            
                            <p style='font-size: 16px; line-height: 1.6; color: #333;'>
                                Great news! Your order has been <strong>confirmed</strong> and is now being prepared for delivery.
                            </p>

                            <div style='background: #e3f2fd; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                                <h3 style='color: #1976d2; margin-top: 0;'>📦 Order Details</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; border-bottom: 1px solid #ddd; font-weight: bold;'>Order Number:</td>
                                        <td style='padding: 8px 0; border-bottom: 1px solid #ddd;'>#{order.Id}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; border-bottom: 1px solid #ddd; font-weight: bold;'>Total Amount:</td>
                                        <td style='padding: 8px 0; border-bottom: 1px solid #ddd; color: #28a745; font-weight: bold;'>Rs. {order.TotalAmount:F2}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; border-bottom: 1px solid #ddd; font-weight: bold;'>Delivery Address:</td>
                                        <td style='padding: 8px 0; border-bottom: 1px solid #ddd;'>{order.ShippingAddress}, {order.ShippingCity}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Order Date:</td>
                                        <td style='padding: 8px 0;'>{order.OrderDate:MMM dd, yyyy 'at' HH:mm}</td>
                                    </tr>
                                </table>
                            </div>

                            <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                                <h3 style='color: #856404; margin-top: 0;'>🚚 Delivery Information</h3>
                                <p style='color: #856404; margin: 0; font-size: 16px;'>
                                    <strong>Estimated Delivery Time:</strong> {deliveryTime:MMM dd, yyyy 'at' HH:mm}<br>
                                    <strong>Delivery Window:</strong> Approximately {etaMinutes} minutes from now<br>
                                    <strong>Status:</strong> <span style='color: #28a745; font-weight: bold;'>Confirmed & Processing</span>
                                </p>
                            </div>

                            <div style='background: #d4edda; border: 1px solid #c3e6cb; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                                <h3 style='color: #155724; margin-top: 0;'>✅ What happens next?</h3>
                                <ul style='color: #155724; margin: 0; padding-left: 20px;'>
                                    <li>Our team is preparing your order right now</li>
                                    <li>You'll receive an SMS when your order is dispatched</li>
                                    <li>Our delivery partner will contact you before arrival</li>
                                    <li>Keep your phone handy for delivery updates</li>
                                </ul>
                            </div>

                            <div style='text-align: center; margin: 30px 0;'>
                                <p style='color: #666; font-size: 14px; margin: 0;'>
                                    Questions about your order? Contact us at<br>
                                    📧 support@getinstantmart.com | 📞 +977-XXX-XXXX
                                </p>
                            </div>

                            <div style='border-top: 2px solid #eee; padding-top: 20px; text-align: center;'>
                                <p style='color: #28a745; font-size: 18px; font-weight: bold; margin: 0;'>
                                    Thank you for choosing GetInstantMart! 🛒
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