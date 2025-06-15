using Application.Common;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.NotificationFeat.Command;
public record SendNotificationToUser
(
    int Id,
    int UserId,
    string Email,
    int OrderId,
    string Title,
    string Message

) : IRequest<IResult>;

public class SendNotificationToUserCommandHandler : IRequestHandler<SendNotificationToUser, IResult>
{
    private readonly INotificationRabbitMqPublisher _rabbitMqPublisher;
    public SendNotificationToUserCommandHandler(
        INotificationRabbitMqPublisher rabbitMqPublisher)
    {
        _rabbitMqPublisher = rabbitMqPublisher;
    }
    public async Task<IResult> Handle(SendNotificationToUser notification, CancellationToken cancellationToken)
    {
        try
        {
            if (notification == null)
            {
                return Results.NotFound("Notification data is null.");
            }
            var notif = new
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Email = notification.Email,
                OrderId = notification.OrderId,
                Title = notification.Title,
                Message = notification.Message,
                Type = "OrderConfirmed",
                OrderDate = DateTime.Now,
            };
            _rabbitMqPublisher.PublishMessage(notif);
            return Results.Ok(new
            {
                data=notif,
                Message="Notif sent successfully."
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error processing notification: {ex.Message}");
        }
        
    }
}