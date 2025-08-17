using Application.Dto.NotificationDTOs;

namespace Infrastructure.Persistence.Services;

public class NotificationService : INotificationService
{
    private const int ATTEMPTS = 3;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly INotificationRabbitMqPublisher _rabbitMqPublisher;
    public NotificationService(IHttpClientFactory httpClientFactory, IConfiguration config, INotificationRabbitMqPublisher rabbitMqPublisher)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _rabbitMqPublisher = rabbitMqPublisher;
    }
    public async Task SendToAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SendToUserAsync(string email, Notification notification)
    {
        using var client = _httpClientFactory.CreateClient();
        bool isSent = false;
        bool isRead = false;
        try
        {
            var notif = new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Email = email,
                OrderId = notification.OrderId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                IsRead = isRead,
                OrderDate = notification.CreatedAt,
            };
            _rabbitMqPublisher.PublishMessage(notif);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending notification to user {email}: {e.Message}");
            throw;
        }

    }
    public async Task SendToAdminAsync(string email, Notification notification)
    {
        using var client = _httpClientFactory.CreateClient();
        bool isSent = false;
        bool isRead = false;
        try
        {
            var notif = new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Email = email,
                OrderId = notification.OrderId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                IsRead = isRead,
                OrderDate = notification.CreatedAt,
            };
            _rabbitMqPublisher.PublishMessage(notif);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending notification to user {email}: {e.Message}");
            throw;
        }

    }
}
