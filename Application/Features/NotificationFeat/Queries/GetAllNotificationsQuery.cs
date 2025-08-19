using Application.Dto.NotificationDTOs;
using Application.Interfaces.Repositories;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.NotificationFeat.Queries;
public record GetAllNotificationsQuery(int PageNumber, int PageSize, string? Status) : IRequest<IResult>;

public class GetAllNotificationsQueryHandler : IRequestHandler<GetAllNotificationsQuery, IResult>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<GetAllNotificationsQueryHandler> _logger;
    public GetAllNotificationsQueryHandler(
        INotificationRepository NotificationRepository,
        ILogger<GetAllNotificationsQueryHandler> logger)
    {
        _notificationRepository = NotificationRepository;
        _logger = logger;
    }

    public async Task<IResult> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{FunctionName}, received request to retrieve notifications",
            nameof(GetAllNotificationsQueryHandler));
        try
        {
            var notifications = await _notificationRepository.Queryable.AsNoTracking().
                Include(n => n.User).
                Where(un => string.IsNullOrEmpty(request.Status) || un.Status.ToString().ToLower() == request.Status.ToLower()).
                Where(un => !un.IsDeleted).
                OrderByDescending(un => un.CreatedAt).
                Select(un => new NotificationDto
                {
                    Id = un.Id,
                    UserId = un.UserId,
                    OrderId = un.OrderId,
                    Title = un.Title,
                    Message = un.Message,
                    Type = un.Type.ToString(),
                    Email = un.User!.Email,
                    Status = un.Status.ToString(),
                    IsRead = un.Status == NotificationStatus.Read,
                    OrderDate = un.CreatedAt
                }).
                Skip((request.PageNumber - 1) * request.PageSize).
                Take(request.PageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                Response = notifications,
                Meta = new
                {
                    TotalCount = await _notificationRepository.Queryable.AsNoTracking()
                        .Where(un => string.IsNullOrEmpty(request.Status) || un.Status.ToString() == request.Status)
                        .Where(un => !un.IsDeleted)
                        .CountAsync(cancellationToken),
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                },
                Message = "Successfully retrieved  notifications"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{FunctionName}, failed to retrieve  Notifications",
                nameof(GetAllNotificationsQueryHandler));
            return Results.BadRequest(new
            {
                Message = "Failed to retrieve  Notifications. Please try again."
            });
        }
    }

}

