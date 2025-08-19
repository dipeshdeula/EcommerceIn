using Application.Interfaces.Repositories;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.NotificationFeat.Commands;

public record AcknowledgeNotificationCommand(int notificaitonId) : IRequest<IResult>;

public class AcknowledgeNotificationCommandHandler : IRequestHandler<AcknowledgeNotificationCommand, IResult>
{
    private readonly INotificationRepository _notificationRepository;

    public AcknowledgeNotificationCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IResult> Handle(AcknowledgeNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationRepository.GetByIdAsync(request.notificaitonId);
            if (result is null)
            {
                return Results.NotFound(new { Message = "Notification not found." });
            }
            result.Status = NotificationStatus.Acknowledged;
            await _notificationRepository.UpdateAsync(result);
            await _notificationRepository.SaveChangesAsync();
            return Results.Ok("Notification Acknowledged.");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error acknowledging notification: {ex.Message}");
        }
    }
}
