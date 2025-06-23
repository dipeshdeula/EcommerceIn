using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.NotificationFeat.Commands;

public record MarkNotificationsAsReadCommand(ICollection<int> notificaitonIds) : IRequest<IResult>;

public class MarkNotificationsAsReadCommandHandler : IRequestHandler<MarkNotificationsAsReadCommand, IResult>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationsAsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IResult> Handle(MarkNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        if (request.notificaitonIds == null || !request.notificaitonIds.Any())
        {
            return Results.BadRequest("No notification IDs provided");
        }

        try
        {
            // First verify all notifications exist
            var notifications = await _notificationRepository.Queryable.
                Where(n => request.notificaitonIds.Contains(n.Id)).ToListAsync(cancellationToken);

            if (notifications is null || notifications.Count == 0)
            {
                return Results.NotFound("Notifications were not found");
            }            
            foreach(var notification in notifications)
            {

               // Update the status to Read
                notification.Status = NotificationStatus.Read;
            }
            await _notificationRepository.UpdateRangeAsync(notifications);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                Message = "Notifications marked as read",
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error marking notifications as read: {ex.Message}");
        }
    }
}

