using Application.Features.OrderFeat.Events;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services;

public class NotificationService : INotificationService
{
    private const int ATTEMPTS = 3;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    public NotificationService(IHttpClientFactory httpClientFactory, IConfiguration config, IRabbitMqPublisher rabbitMqPublisher)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _rabbitMqPublisher = rabbitMqPublisher;
    }
    public async Task SendToAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SendToUserAsync(string username, Notification notification)
    {
        using var client = _httpClientFactory.CreateClient();
        bool isSent = false;

        try
        {

            var notif = new
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Username = username,
                OrderId = notification.OrderId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                OrderDate = notification.CreatedAt,
            };
            _rabbitMqPublisher.Publish("OrderPlacedQueue", notif, Guid.NewGuid().ToString(), null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending notification to user {username}: {e.Message}");
            throw;
        }

    }
}
