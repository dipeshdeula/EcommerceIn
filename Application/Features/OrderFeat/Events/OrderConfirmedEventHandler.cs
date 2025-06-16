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

namespace Application.Features.OrderFeat.Events;


public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<OrderConfirmedEventHandler> logger)
    {
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent orderConfirmedEvent, CancellationToken cancellationToken)
    {
        try
        {

            var notification = new Notification
            {
                OrderId = orderConfirmedEvent.Order.Id,
                UserId = orderConfirmedEvent.User.Id,

                Title = "Order Confirmed",
                Message = $"Your order #{orderConfirmedEvent.Order.Id} has been confirmed and will be delivered in approximately {orderConfirmedEvent.EtaMinutes} minutes.",
                Type = NotificationType.OrderConfirmed,
                Status = NotificationStatus.Pending
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            var email = orderConfirmedEvent.User.Email;
            await _notificationService.SendToUserAsync(email, notification);

            _logger.LogInformation("Notification {NotificationId} created successfully", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OrderConfirmedEvent");
        }
    }
}
