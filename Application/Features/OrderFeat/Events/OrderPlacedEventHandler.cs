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

public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<OrderPlacedEventHandler> logger)
    {
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(OrderPlacedEvent orderEvent, CancellationToken cancellationToken)
    {
        try
        {

            var notification = new Notification
            {
                OrderId = orderEvent.Order.Id,
                UserId = orderEvent.User.Id,

                Title = "Order Placed",
                Message = $"Order #{orderEvent.Order.Id} has been placed and by user #{orderEvent.User.Id}.",
                Type = NotificationType.OrderPlaced,
                Status = NotificationStatus.Pending
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            var email = orderEvent.User.Email;
            await _notificationService.SendToUserAsync(email, notification);

            _logger.LogInformation("Notification {NotificationId} created successfully", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OrderPlacedEvent");
        }
    }
}
