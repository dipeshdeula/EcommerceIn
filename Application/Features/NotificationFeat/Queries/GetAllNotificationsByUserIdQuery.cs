using Application.Dto.NotificationDTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Features.NotificationFeat.Queries;

public record GetAllNotificationsByUserIdQuery (
    int userId,
    int PageNumber,
    int PageSize
) : IRequest<IResult>;

public class GetAllNotificationsByUserIdQueryHandler : IRequestHandler<GetAllNotificationsByUserIdQuery, IResult>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;

    public GetAllNotificationsByUserIdQueryHandler(INotificationRepository notificationRepository, IUserRepository userRepository)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
    }

    public async Task<IResult> Handle(GetAllNotificationsByUserIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.userId);
            if (user is null)
            {
                return Results.NotFound("User not found.");
            }
            // if user role is User, filter by OrderConfirmed notifications, otherwise filter by OrderPlaced notifications
            Expression<Func<Notification,bool>> pred = (user.Role == UserRoles.User) ? (n => n.Type == NotificationType.OrderConfirmed) : (n => n.Type == NotificationType.OrderPlaced);

            var notifications = await _notificationRepository.Queryable.AsNoTracking().
                Include(n => n.User).
                Where(n => n.UserId == request.userId).
                Where(n => !n.IsDeleted).
                Where(pred).
                Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Email = n.User.Email,
                    OrderId = n.OrderId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    Status = n.Status.ToString(),
                    IsRead = n.Status == NotificationStatus.Read,
                    OrderDate = n.CreatedAt
                }).
                Skip((request.PageNumber - 1) * request.PageSize).
                Take(request.PageSize).
                OrderByDescending(n => n.OrderDate).
                ToListAsync();
            if (notifications == null || !notifications.Any())
            {
                return Results.NotFound("No notifications found for the specified user.");
            }
            return Results.Ok(notifications);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving notifications: {ex.Message}");
        }
    }
}