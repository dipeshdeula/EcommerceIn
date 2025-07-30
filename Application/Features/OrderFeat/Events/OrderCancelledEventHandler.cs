using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.Events
{
    public class OrderCancelledEventHandler : INotificationHandler<OrderCancelledEvents>
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<OrderCancelledEventHandler> _logger;

        public OrderCancelledEventHandler(
            INotificationService notificationService,
            INotificationRepository notificationRepository,
            ILogger<OrderCancelledEventHandler> logger
            )
        {
            _notificationService = notificationService;
            _notificationRepository = notificationRepository;
            _logger = logger;
            
        }
        public async Task Handle(OrderCancelledEvents orderCancelledEvent, CancellationToken cancellationToken)
        {
            try {
                var notification = new Notification
                {
                    OrderId = orderCancelledEvent.Order.Id,
                    UserId = orderCancelledEvent.User.Id,

                    Title = "Order Cancelled",
                    Message = $"Your order #{orderCancelledEvent.Order.Id} has been cancelled as per your request. Thank you for visit us.",
                    Type = NotificationType.OrderCancelled,
                    Status = NotificationStatus.Pending
                };

                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync(cancellationToken);

                var email = orderCancelledEvent.User.Email;
                await _notificationService.SendToUserAsync(email, notification);

                _logger.LogInformation("Notification {NotificationId} created successfully", notification.Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle OrderConfirmedEvent");
            }
        }
    }
}
